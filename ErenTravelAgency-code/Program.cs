using ErenTravel3API.Data;
using ErenTravel3API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "frontend"
});

builder.Services.AddCors(options => options
    .AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod())
);

builder.Services.AddControllers();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPackageService, PackageService>();

// Local fallback and hosting variables.
var host = Environment.GetEnvironmentVariable("MYSQLHOST") ?? "localhost";
var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
var user = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "root";
var password = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? "";
var database = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "ErenTravelDb";
var sslMode = Environment.GetEnvironmentVariable("MYSQL_SSL_MODE") ?? (host == "localhost" ? "Preferred" : "Required");

var connectionString = $"server={host};port={port};user id={user};password={password};database={database};default command timeout=600;connect timeout=600;AllowZeroDateTime=True;ConvertZeroDateTime=True;SslMode={sslMode}";
var serverVersion = new MySqlServerVersion(new Version(9, 2, 0));

builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    })
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment())
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TravelDbContext>();

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS AppUsers (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            FullName VARCHAR(100) NOT NULL,
            Email VARCHAR(150) NOT NULL,
            Password VARCHAR(100) NOT NULL,
            Role VARCHAR(30) NOT NULL,
            AgencyName VARCHAR(150) NULL,
            TaxId VARCHAR(80) NULL,
            Address VARCHAR(200) NULL,
            Phone VARCHAR(50) NULL,
            SecurityQuestion VARCHAR(250) NULL,
            SecurityAnswerHash VARCHAR(100) NULL,
            IsApproved BIT NOT NULL DEFAULT 0,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
            UNIQUE KEY IX_AppUsers_Email (Email)
        );
    ");

    var hasSecurityQuestionColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AppUsers'
          AND COLUMN_NAME = 'SecurityQuestion';").AsEnumerable().First();

    if (hasSecurityQuestionColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE AppUsers ADD COLUMN SecurityQuestion VARCHAR(250) NULL;");

    var hasSecurityAnswerHashColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AppUsers'
          AND COLUMN_NAME = 'SecurityAnswerHash';").AsEnumerable().First();

    if (hasSecurityAnswerHashColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE AppUsers ADD COLUMN SecurityAnswerHash VARCHAR(100) NULL;");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Customers (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            Emri VARCHAR(100) NOT NULL,
            Email VARCHAR(150) NOT NULL,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
            UNIQUE KEY IX_Customers_Email (Email)
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Packages (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            Paketa VARCHAR(200) NOT NULL,
            Kohezgjatja INT NOT NULL,
            Cmimi DECIMAL(18,2) NOT NULL,
            TravelDate DATETIME(6) NULL,
            Pershkrimi VARCHAR(500) NULL,
            AgencyName VARCHAR(150) NULL,
            ImageUrl VARCHAR(500) NULL,
            MaxPersons INT NOT NULL DEFAULT 150,
            IsAvailable BIT NOT NULL DEFAULT 1
        );
    ");

    var hasAgencyColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Packages'
          AND COLUMN_NAME = 'AgencyName';").AsEnumerable().First();

    if (hasAgencyColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Packages ADD COLUMN AgencyName VARCHAR(150) NULL;");

    var hasImageColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Packages'
          AND COLUMN_NAME = 'ImageUrl';").AsEnumerable().First();

    if (hasImageColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Packages ADD COLUMN ImageUrl VARCHAR(500) NULL;");

    var hasTravelDateColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Packages'
          AND COLUMN_NAME = 'TravelDate';").AsEnumerable().First();

    if (hasTravelDateColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Packages ADD COLUMN TravelDate DATETIME(6) NULL;");

    var hasMaxPersonsColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Packages'
          AND COLUMN_NAME = 'MaxPersons';").AsEnumerable().First();

    if (hasMaxPersonsColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Packages ADD COLUMN MaxPersons INT NOT NULL DEFAULT 150;");

    db.Database.ExecuteSqlRaw("UPDATE Packages SET MaxPersons = 150 WHERE MaxPersons IS NULL OR MaxPersons <= 0;");

    var hasReservationTable = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations';").AsEnumerable().First();

    if (hasReservationTable > 0)
    {
        var hasPackageIdColumn = db.Database.SqlQueryRaw<int>(@"
            SELECT COUNT(*) AS Value
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'Reservations'
              AND COLUMN_NAME = 'PackageId';").AsEnumerable().First();

        // Older versions may have an incomplete Reservations table.
        // Rebuild only this table when PackageId is missing.
        if (hasPackageIdColumn == 0)
            db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Reservations;");
    }

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Reservations (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            CustomerId INT NOT NULL,
            PackageId INT NOT NULL,
            DataNisjes DATETIME(6) NOT NULL,
            DataKthimit DATETIME(6) NOT NULL,
            NrPersonave INT NOT NULL,
            NrDhomave INT NOT NULL,
            Cmimi DECIMAL(18,2) NOT NULL,
            IsConfirmed BIT NOT NULL DEFAULT 0,
            IsCancelled BIT NOT NULL DEFAULT 0,
            CancelledAt DATETIME(6) NULL,
            CancellationReason VARCHAR(500) NULL,
            CancellationFee DECIMAL(18,2) NOT NULL DEFAULT 0,
            RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
            RefundStatus VARCHAR(50) NULL,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
            CONSTRAINT FK_Reservations_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE CASCADE,
            CONSTRAINT FK_Reservations_Packages_PackageId FOREIGN KEY (PackageId) REFERENCES Packages(Id) ON DELETE RESTRICT
        );
    ");

    var hasReservationCancelledColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'IsCancelled';").AsEnumerable().First();

    if (hasReservationCancelledColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN IsCancelled BIT NOT NULL DEFAULT 0;");

    var hasReservationCancelledAtColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'CancelledAt';").AsEnumerable().First();

    if (hasReservationCancelledAtColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN CancelledAt DATETIME(6) NULL;");

    var hasReservationCancelReasonColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'CancellationReason';").AsEnumerable().First();

    if (hasReservationCancelReasonColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN CancellationReason VARCHAR(500) NULL;");

    var hasCancellationFeeColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'CancellationFee';").AsEnumerable().First();

    if (hasCancellationFeeColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN CancellationFee DECIMAL(18,2) NOT NULL DEFAULT 0;");

    var hasRefundAmountColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'RefundAmount';").AsEnumerable().First();

    if (hasRefundAmountColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0;");

    var hasRefundStatusColumn = db.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(*) AS Value
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND COLUMN_NAME = 'RefundStatus';").AsEnumerable().First();

    if (hasRefundStatusColumn == 0)
        db.Database.ExecuteSqlRaw("ALTER TABLE Reservations ADD COLUMN RefundStatus VARCHAR(50) NULL;");


    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS AgencyFollows (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            UserEmail VARCHAR(150) NOT NULL,
            AgencyName VARCHAR(150) NOT NULL,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
            UNIQUE KEY IX_AgencyFollows_User_Agency (UserEmail, AgencyName)
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS Reviews (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            UserEmail VARCHAR(150) NOT NULL,
            UserName VARCHAR(100) NOT NULL,
            AgencyName VARCHAR(150) NOT NULL,
            PackageId INT NULL,
            Rating INT NOT NULL,
            Comment VARCHAR(700) NULL,
            IsReported BIT NOT NULL DEFAULT 0,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS IssueReports (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            ReporterEmail VARCHAR(150) NULL,
            TargetType VARCHAR(50) NOT NULL,
            TargetId INT NULL,
            AgencyName VARCHAR(150) NULL,
            Reason VARCHAR(700) NOT NULL,
            Status VARCHAR(40) NOT NULL DEFAULT 'Open',
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
        );
    ");

    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ActivityLogs (
            Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            ActionType VARCHAR(80) NOT NULL,
            UserEmail VARCHAR(150) NULL,
            UserRole VARCHAR(50) NULL,
            AgencyName VARCHAR(150) NULL,
            Status VARCHAR(40) NOT NULL,
            Description VARCHAR(1000) NULL,
            ErrorMessage VARCHAR(1000) NULL,
            CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
        );
    ");


    var packageCount = db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Packages;").AsEnumerable().First();

    // Sync existing packages with the original frontend images.
    // This keeps Eren Travel packages linked with the correct images.
    db.Database.ExecuteSqlRaw(@"
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/destinacion1.jpeg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Budapest – Hungari';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package9.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Pragë – Çeki';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package10.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Dubai – Emiratet';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package14.jpeg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Paris – Francë' AND Cmimi = 399;
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package11.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'DisneyLand Paris – Francë';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package1.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Venecia, Verona – Itali';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package2.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Firence – Itali';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package3.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Bruge – Belgjikë';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package4.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Stockholm – Suedi';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package5.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Strasburg - Francë • Zyrih - Zvicërr';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package6.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Arabi Saudite';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package7.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Jordani';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/package8.jpg', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Bukuresht – Rumani';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/festa1.png', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Paris – Francë' AND Cmimi = 419;
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/festa2.PNG', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Barcelonë – Spanjë';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/festa3.PNG', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Alberobello – Itali';
        UPDATE Packages SET AgencyName = 'Eren Travel', ImageUrl = '/images/festa4.PNG', MaxPersons = IF(MaxPersons IS NULL OR MaxPersons <= 0, 150, MaxPersons) WHERE Paketa = 'Korçë – Shqipëri';
    ");

    // Default Travel Agent for Eren Travel.
    // It can manage the current Eren Travel packages.
    db.Database.ExecuteSqlRaw(@"
        INSERT INTO AppUsers (FullName, Email, Password, Role, AgencyName, TaxId, Address, Phone, SecurityQuestion, SecurityAnswerHash, IsApproved)
        SELECT 'Eren Travel Agent', 'erentravel@grupierenit.com', '3D0B731B351FB84EC09405BB635EDA4B7147454C36FB4940D576EA5C950D9DD4', 'TravelAgent', 'Eren Travel', 'EREN-TRAVEL-TAX', 'Tirana, Albania', '+355000000000', 'Cili është emri i agjencisë?', 'A67A2FF6BA5E4583089691B6DD1F66D28F7E70929F1D8D36BB1FAE6F450955EB', 1
        WHERE NOT EXISTS (SELECT 1 FROM AppUsers WHERE Email = 'erentravel@grupierenit.com');
    ");

    db.Database.ExecuteSqlRaw("UPDATE AppUsers SET Password = '3D0B731B351FB84EC09405BB635EDA4B7147454C36FB4940D576EA5C950D9DD4' WHERE Email = 'erentravel@grupierenit.com' AND Password = 'Eren123';");
    db.Database.ExecuteSqlRaw("UPDATE AppUsers SET SecurityQuestion = 'Cili është emri i agjencisë?', SecurityAnswerHash = 'A67A2FF6BA5E4583089691B6DD1F66D28F7E70929F1D8D36BB1FAE6F450955EB' WHERE Email = 'erentravel@grupierenit.com' AND (SecurityAnswerHash IS NULL OR SecurityAnswerHash = '');");

    if (packageCount == 0)
    {
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO Packages (Paketa, Kohezgjatja, Cmimi, TravelDate, Pershkrimi, AgencyName, ImageUrl, MaxPersons, IsAvailable) VALUES
            ('Budapest – Hungari', 5, 299, DATE_ADD(CURRENT_DATE(), INTERVAL 13 DAY), 'Fluturim + Hotel • Nisja nga Tirana', 'Eren Travel', '/images/destinacion1.jpeg', 150, 1),
            ('Pragë – Çeki', 4, 305, DATE_ADD(CURRENT_DATE(), INTERVAL 16 DAY), 'Hotel 4★ • Vizita panoramike', 'Eren Travel', '/images/package9.jpg', 150, 1),
            ('Dubai – Emiratet', 5, 699, DATE_ADD(CURRENT_DATE(), INTERVAL 19 DAY), 'Guide • Hotel 4★ • Safari në shkretëtirë', 'Eren Travel', '/images/package10.jpg', 150, 1),
            ('Paris – Francë', 4, 399, DATE_ADD(CURRENT_DATE(), INTERVAL 22 DAY), 'Guidë • Hotel • Qyteti i dritave', 'Eren Travel', '/images/package14.jpeg', 150, 1),
            ('DisneyLand Paris – Francë', 5, 499, DATE_ADD(CURRENT_DATE(), INTERVAL 25 DAY), 'Guide • Hotel 4★ • Argëtim familjar', 'Eren Travel', '/images/package11.jpg', 150, 1),
            ('Venecia, Verona – Itali', 4, 344, DATE_ADD(CURRENT_DATE(), INTERVAL 28 DAY), 'Guide • Hotel 4★ • Qytetet romantike', 'Eren Travel', '/images/package1.jpg', 150, 1),
            ('Firence – Itali', 2, 99, DATE_ADD(CURRENT_DATE(), INTERVAL 31 DAY), 'Hotel 3★ • Arti dhe kultura', 'Eren Travel', '/images/package2.jpg', 150, 1),
            ('Bruge – Belgjikë', 3, 179, DATE_ADD(CURRENT_DATE(), INTERVAL 34 DAY), 'Fluturim + Hotel + Mëngjes', 'Eren Travel', '/images/package3.jpg', 150, 1),
            ('Stockholm – Suedi', 4, 149, DATE_ADD(CURRENT_DATE(), INTERVAL 37 DAY), 'Fluturim + Hotel • Kryeqyteti skandinav', 'Eren Travel', '/images/package4.jpg', 150, 1),
            ('Strasburg - Francë • Zyrih - Zvicërr', 4, 419, DATE_ADD(CURRENT_DATE(), INTERVAL 40 DAY), 'Guide • Hotel 4★ • Dy vende', 'Eren Travel', '/images/package5.jpg', 150, 1),
            ('Arabi Saudite', 7, 699, DATE_ADD(CURRENT_DATE(), INTERVAL 43 DAY), 'Guide • Hotel 4★ • Eksplorim kulturor', 'Eren Travel', '/images/package6.jpg', 150, 1),
            ('Jordani', 6, 1199, DATE_ADD(CURRENT_DATE(), INTERVAL 46 DAY), 'Guide • Hotel 4★ • Petra dhe më shumë', 'Eren Travel', '/images/package7.jpg', 150, 1),
            ('Bukuresht – Rumani', 4, 175, DATE_ADD(CURRENT_DATE(), INTERVAL 49 DAY), 'Fluturim + Hotel 3★', 'Eren Travel', '/images/package8.jpg', 150, 1),
            ('Barcelonë – Spanjë', 4, 399, DATE_ADD(CURRENT_DATE(), INTERVAL 52 DAY), 'Fluturim + Hotel • Oferta fundviti', 'Eren Travel', '/images/festa2.PNG', 150, 1),
            ('Alberobello – Itali', 3, 135, DATE_ADD(CURRENT_DATE(), INTERVAL 55 DAY), 'Fluturim + Hotel • Fshati karakteristik', 'Eren Travel', '/images/festa3.PNG', 150, 1),
            ('Korçë – Shqipëri', 3, 249, DATE_ADD(CURRENT_DATE(), INTERVAL 58 DAY), 'Hotel Kocibelli • Viti i Ri tradicional', 'Eren Travel', '/images/festa4.PNG', 150, 1);
        ");
    }
}

app.Run();

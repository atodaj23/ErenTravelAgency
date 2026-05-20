# Eren Travel Management System - Functional Version

Ky version bazohet te dokumentet/requirements të ZIP-it të projektit dhe i lidh funksionalitetet me web-in ekzistues.

## Roli Client
- Kërkon paketa sipas destinacionit/agjencisë, çmimit dhe datës së udhëtimit.
- Hap detajet e plota të paketës.
- Bën rezervim direkt nga paketa e hapur.
- Sheh historikun e rezervimeve te `Rezervimet`.
- Ndjek agjenci.
- Lë reviews dhe ratings.
- Raporton problem te një paketë.

## Roli B2B
- Regjistrohet me të dhëna biznesi/agjencie dhe Tax ID.
- Duhet aprovuar nga admini.
- Sheh çmim B2B me 10% ulje.
- Rezervon me çmim business.

## Roli Travel Agent
- Publikon paketa me foto.
- Përditëson ose fshin paketat e veta.
- Menaxhon/aprovon rezervimet për agjencinë e vet.
- Sheh reviews dhe feedback nga klientët.
- Sheh performancën e ofertave: rezervime, xhiro, rating.

## Roli Administrator
- Aprovon/fshin userat.
- Monitoron të gjitha rezervimet.
- Monitoron agjencitë dhe xhiron.
- Sheh reviews dhe raportimet/problemet.
- Pastron të dhëna të agjencive të fshira.

## Hapja e projektit
1. Starto MySQL nga XAMPP.
2. Në terminal:

```powershell
cd ErenTravelAgency-main
dotnet run
```

3. Hape në browser:

```text
http://localhost:5098
```

## Admin
Email: `admin@grupierenit.com`
Password: `Grupi123`

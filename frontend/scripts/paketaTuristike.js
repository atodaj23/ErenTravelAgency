// API Base URL
const API_BASE_URL = "http://localhost:5098/api";

// Handle Rezervo form submission
function submitReservation(event) {
  event.preventDefault();

  const name = $("#name").val();
  const email = $("#email").val();
  const packageName = $("#packageName").val();
  const startDate = $("#startDate").val();
  const endDate = $("#endDate").val();
  const numPeople = parseInt($("#numPeople").val());
  const numRooms = parseInt($("#numRooms").val());
  const basePrice = parseFloat($("#basePricePerPerson").val());
  const totalPrice = basePrice * numPeople;

  if (!name || !email || !packageName || !startDate || !endDate) {
    alert("Ju lutem plotësoni të gjitha fushat!");
    return;
  }

  // Create Reservation object
  const reservation = {
    emri: name,
    email: email,
    paketa: packageName,
    dataNisjes: startDate,
    dataKthimit: endDate,
    nrPersonave: numPeople,
    nrDhomave: numRooms,
    cmimi: totalPrice,
  };

  // Show loading state
  const submitButton = $('#rezervoForm button[type="submit"]');
  const originalText = submitButton.text();
  submitButton.prop("disabled", true).text("Duke rezervuar...");

  // Send to API
  fetch(`${API_BASE_URL}/reservations`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(reservation),
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error("Network response was not ok");
      }
      return response.json();
    })
    .then((data) => {
      // Show success message
      alert(
        'Rezervimi për "' +
          packageName +
          '" u shtua me sukses! Çmimi total: ' +
          totalPrice +
          "€",
      );

      // Reset form
      $("#rezervoForm")[0].reset();
      $("#displayPrice").text("0€");
      $("#packagePrice").val("0");
      $("#basePricePerPerson").val("0");

      // Close modal
      const modalElement = document.getElementById("rezervoModal");
      const modal = bootstrap.Modal.getInstance(modalElement);
      if (modal) {
        modal.hide();
      }
    })
    .catch((error) => {
      console.error("Error:", error);
      alert("Ndodhi një gabim gjatë rezervimit. Ju lutem provoni përsëri.");
    })
    .finally(() => {
      // Reset button state
      submitButton.prop("disabled", false).text(originalText);
    });
}

// Load packages from API
function loadPackages() {
  fetch(`${API_BASE_URL}/packages`)
    .then((response) => response.json())
    .then((packages) => {
      console.log("Packages loaded from API:", packages);
    })
    .catch((error) => {
      console.error("Error loading packages:", error);
    });
}

// Initialize on page load
$(document).ready(function () {
  // Load packages from API
  loadPackages();

  // Set minimum date to today
  const today = new Date().toISOString().split("T")[0];
  $("#startDate, #endDate").attr("min", today);

  // Check if we need to scroll to a specific package
  const hash = window.location.hash;
  if (hash && hash.startsWith("#package-")) {
    setTimeout(function () {
      const element = document.querySelector(hash);
      if (element) {
        element.scrollIntoView({ behavior: "smooth", block: "center" });
      }
    }, 100);
  }

  // Handle "Rezervo" button clicks
  $(".rezervo-btn").on("click", function (e) {
    e.preventDefault();

    const card = $(this).closest(".card");
    const packageName = card.find("h6").text().trim();
    const priceText = card.find(".small").text();

    // Extract price from text
    const priceMatch = priceText.match(/(\d+)€/);
    let basePrice = 0;
    if (priceMatch) {
      basePrice = parseInt(priceMatch[1]);
    }

    // Set form values
    $("#packageName").val(packageName);
    $("#basePricePerPerson").val(basePrice);
    $("#numPeople").val(1);
    $("#numRooms").val(1);
    $("#displayPrice").text(basePrice + "€");
    $("#packagePrice").val(basePrice);
    $("#startDate").val("");
    $("#endDate").val("");

    // Show modal
    const modalElement = document.getElementById("rezervoModal");
    const modal = new bootstrap.Modal(modalElement);
    modal.show();
  });

  // Update price when number of people changes
  $("#numPeople").on("change", function () {
    const basePrice = parseFloat($("#basePricePerPerson").val());
    const numPeople = parseInt($(this).val());
    const totalPrice = basePrice * numPeople;

    $("#displayPrice").text(totalPrice + "€");
    $("#packagePrice").val(totalPrice);
  });

  // Handle form submission
  $("#rezervoForm").on("submit", submitReservation);
});

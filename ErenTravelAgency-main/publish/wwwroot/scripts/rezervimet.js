// API Base URL 
const API_BASE_URL = 'http://localhost:5098/api';

let reservations = [];

// Load reservations from API
function loadReservations() {
    const container = document.getElementById('reservation-list');
    container.innerHTML = '<div class="col-12 text-center"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';

    fetch(`${API_BASE_URL}/reservations`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            reservations = data;
            renderReservations();
        })
        .catch(error => {
            console.error('Error loading reservations:', error);
            container.innerHTML = '<div class="col-12"><p class="text-center text-danger fs-5 mt-5">Ndodhi një gabim gjatë ngarkimit të rezervimeve.</p></div>';
        });
}

// Display all reservations
function renderReservations() {
    const container = document.getElementById('reservation-list');
    container.innerHTML = '';

    if(reservations.length === 0) {
        container.innerHTML = '<div class="col-12"><p class="text-center text-muted fs-5 mt-5">Nuk ka rezervime të bëra.</p></div>';
        return;
    }

    // Loop through each reservation and create HTML
    for(let i = 0; i < reservations.length; i++) {
        const res = reservations[i];
        
        // Format dates
        const dataNisjes = new Date(res.dataNisjes).toLocaleDateString('sq-AL');
        const dataKthimit = new Date(res.dataKthimit).toLocaleDateString('sq-AL');
        
        // Determine status
        let statusBadge = '';
        let cardBorder = '';
        if (res.confirmed) {
            statusBadge = '<span class="badge bg-success">E Konfirmuar</span>';
            cardBorder = 'border-success';
        } else {
            statusBadge = '<span class="badge bg-warning text-dark">Në pritje</span>';
            cardBorder = 'border-light';
        }
        
        // Create card HTML
        const cardHTML = '<div class="col-lg-6 col-md-12">' +
            '<div class="card shadow ' + cardBorder + '">' +
                '<div class="card-header bg-primary text-white d-flex justify-content-between align-items-center">' +
                    '<h5 class="mb-0">' + res.paketa + '</h5>' +
                    statusBadge +
                '</div>' +
                '<div class="card-body">' +
                    '<div class="row mb-2">' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Emri:</strong></p>' +
                            '<p class="text-muted">' + res.emri + '</p>' +
                        '</div>' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Email:</strong></p>' +
                            '<p class="text-muted">' + res.email + '</p>' +
                        '</div>' +
                    '</div>' +
                    '<div class="row mb-2">' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Data nisjes:</strong></p>' +
                            '<p class="text-muted">' + dataNisjes + '</p>' +
                        '</div>' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Data kthimit:</strong></p>' +
                            '<p class="text-muted">' + dataKthimit + '</p>' +
                        '</div>' +
                    '</div>' +
                    '<div class="row mb-3">' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Nr personave:</strong></p>' +
                            '<p class="text-muted">' + res.nrPersonave + '</p>' +
                        '</div>' +
                        '<div class="col-6">' +
                            '<p class="mb-1"><strong>Nr dhomave:</strong></p>' +
                            '<p class="text-muted">' + res.nrDhomave + '</p>' +
                        '</div>' +
                    '</div>' +
                    '<div class="row mb-3">' +
                        '<div class="col-12">' +
                            '<p class="mb-1"><strong>Çmimi:</strong></p>' +
                            '<p class="text-success fw-bold fs-5">' + res.cmimi + '€</p>' +
                        '</div>' +
                    '</div>' +
                    '<div class="d-flex gap-2 justify-content-end">' +
                        (!res.confirmed ? '<button class="btn btn-success btn-sm" onclick="confirmReservation(' + res.id + ')">Konfirmo</button>' : '') +
                        '<button class="btn btn-danger btn-sm" onclick="deleteReservation(' + res.id + ')">Fshij</button>' +
                    '</div>' +
                '</div>' +
            '</div>' +
        '</div>';
        
        container.innerHTML += cardHTML;
    }

    // Calculate total price
    let totalPrice = 0;
    for(let i = 0; i < reservations.length; i++) {
        totalPrice += reservations[i].cmimi;
    }

    // Add total section
    const totalHTML = '<div class="col-12 mt-4">' +
        '<div class="card shadow border-primary">' +
            '<div class="card-body">' +
                '<div class="row align-items-center">' +
                    '<div class="col-md-6">' +
                        '<h4 class="mb-0">Shuma Totale: <span class="text-primary">' + totalPrice.toFixed(2) + '€</span></h4>' +
                        '<p class="text-muted small mb-0">Total për ' + reservations.length + ' paketa</p>' +
                    '</div>' +
                    '<div class="col-md-6 text-end">' +
                        '<button class="btn btn-success btn-lg" onclick="confirmAllReservations()">Konfirmo të gjitha</button>' +
                    '</div>' +
                '</div>' +
            '</div>' +
        '</div>' +
    '</div>';
    
    container.innerHTML += totalHTML;
}

// Confirm a single reservation
function confirmReservation(id) {
  if (!confirm('A jeni të sigurt që doni të konfirmoni këtë rezervim?')) return;

  fetch(`${API_BASE_URL}/reservations/${id}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ isConfirmed: true })
  })
  .then(response => {
    if (!response.ok) throw new Error('Network response was not ok');
    alert('Rezervimi u konfirmua me sukses!');
    loadReservations();
  })
  .catch(err => {
    console.error(err);
    alert('Ndodhi një gabim gjatë konfirmimit të rezervimit.');
  });
}


// Confirm all reservations
function confirmAllReservations() {
    if (reservations.length === 0) {
        alert('Nuk ka rezervime për të konfirmuar!');
        return;
    }

    if (!confirm('A jeni të sigurt që doni të konfirmoni të gjitha rezervimet?')) {
        return;
    }

    fetch(`${API_BASE_URL}/reservations/confirm-all`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    })
    .then(async response => {
        if (!response.ok) throw new Error(await response.text());
        return;
    })
    .then(data => {
        alert('Të gjitha rezervimet u konfirmuan me sukses!');
        loadReservations(); // Reload the list
    })
    .catch(error => {
        console.error('Error confirming all reservations:', error);
        alert('Ndodhi një gabim gjatë konfirmimit të rezervimeve.');
    });
}

// Delete a reservation
function deleteReservation(id) {
    if(!confirm("A jeni të sigurt që doni të fshini këtë rezervim?")) {
        return;
    }

    fetch(`${API_BASE_URL}/reservations/${id}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
        }
    })
    .then(response => {
        if (!response.ok) throw new Error('Network response was not ok');
        alert('Rezervimi u fshi me sukses!');
        loadReservations();
    })

    .catch(error => {
        console.error('Error deleting reservation:', error);
        alert('Ndodhi një gabim gjatë fshirjes së rezervimit.');
    });
}

// Load reservations when page loads
$(document).ready(function() {
    loadReservations();
});

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'Back Office - RIM Transport';
  
  // Auth state
  isLoggedIn = false;
  loginForm = { email: '', password: '' };
  authError = '';
  currentUser: any = null;

  // View navigation
  activeView = 'dashboard'; // 'dashboard', 'companies', 'buses', 'trips', 'scan'

  // Mock data (Fellback for Demo Mode)
  companiesList = [
    { id: 1, name: 'El Moussafir', phone: '+222 45250001', email: 'contact@elmoussafir.mr', address: 'Carrefour Madrid, Nouakchott', isActive: true },
    { id: 2, name: 'Sonef Mauritanie', phone: '+222 45250002', email: 'contact@sonef.mr', address: 'Avenue Charles de Gaulle, Nouakchott', isActive: true },
    { id: 3, name: 'Sahara Trans', phone: '+222 45250003', email: 'info@saharatrans.mr', address: 'Carrefour BMD, Nouakchott', isActive: false }
  ];

  busesList = [
    { id: 1, companyName: 'El Moussafir', busNumber: '1234AA01', capacity: 30 },
    { id: 2, companyName: 'El Moussafir', busNumber: '5678AA01', capacity: 15 },
    { id: 3, companyName: 'Sonef Mauritanie', busNumber: '9999AB02', capacity: 50 }
  ];

  tripsList = [
    { id: 1, busNumber: '1234AA01', companyName: 'El Moussafir', departureCity: 'Nouakchott', arrivalCity: 'Nouadhibou', departureDate: '2026-06-20', departureTime: '07:00', price: 800, status: 'SCHEDULED' },
    { id: 2, busNumber: '5678AA01', companyName: 'El Moussafir', departureCity: 'Nouakchott', arrivalCity: 'Rosso', departureDate: '2026-06-20', departureTime: '09:00', price: 300, status: 'SCHEDULED' },
    { id: 3, busNumber: '9999AB02', companyName: 'Sonef Mauritanie', departureCity: 'Nouakchott', arrivalCity: 'Atar', departureDate: '2026-06-21', departureTime: '06:30', price: 600, status: 'SCHEDULED' }
  ];

  // Forms
  newCompany = { name: '', phone: '', email: '', address: '' };
  newBus = { busNumber: '', capacity: 30 };
  newTrip = { busId: 1, departureCity: '', arrivalCity: '', departureDate: '', departureTime: '', price: 500 };
  
  // Scanner state
  scanQrInput = '';
  scanResult: any = null;
  scanError = '';

  // Stats dashboard
  stats = {
    totalRevenue: 124500,
    totalReservations: 380,
    activeCompanies: 8,
    totalBuses: 45
  };

  ngOnInit() {
    const savedAdmin = localStorage.getItem('admin_user');
    if (savedAdmin) {
      this.currentUser = JSON.parse(savedAdmin);
      this.isLoggedIn = true;
      this.activeView = 'dashboard';
    }
  }

  handleLogin() {
    this.authError = '';
    const email = this.loginForm.email.toLowerCase();
    const password = this.loginForm.password;

    // Simulation de connexion admin/compagnie
    if (email === 'admin@transports.gov.mr' && password === 'password123') {
      this.currentUser = { name: 'Administrateur Central', email: email, role: 'ADMIN' };
      this.isLoggedIn = true;
      localStorage.setItem('admin_user', JSON.stringify(this.currentUser));
    } else if (email === 'manager@elmoussafir.mr' && password === 'password123') {
      this.currentUser = { name: 'Gérant El Moussafir', email: email, role: 'COMPANY', companyName: 'El Moussafir' };
      this.isLoggedIn = true;
      localStorage.setItem('admin_user', JSON.stringify(this.currentUser));
    } else if (email === 'manager@sonef.mr' && password === 'password123') {
      this.currentUser = { name: 'Gérant Sonef', email: email, role: 'COMPANY', companyName: 'Sonef Mauritanie' };
      this.isLoggedIn = true;
      localStorage.setItem('admin_user', JSON.stringify(this.currentUser));
    } else {
      this.authError = 'Identifiants administrateur ou compagnie incorrects.';
    }
  }

  handleLogout() {
    localStorage.removeItem('admin_user');
    this.isLoggedIn = false;
    this.currentUser = null;
    this.loginForm = { email: '', password: '' };
  }

  setView(view: string) {
    this.activeView = view;
    this.scanResult = null;
    this.scanError = '';
  }

  // Admin features
  addCompany() {
    if (!this.newCompany.name || !this.newCompany.phone || !this.newCompany.email) return;
    
    const id = this.companiesList.length + 1;
    this.companiesList.push({
      id: id,
      name: this.newCompany.name,
      phone: this.newCompany.phone,
      email: this.newCompany.email,
      address: this.newCompany.address || 'Non spécifiée',
      isActive: true
    });

    this.newCompany = { name: '', phone: '', email: '', address: '' };
    this.stats.activeCompanies = this.companiesList.filter(c => c.isActive).length;
  }

  toggleCompany(company: any) {
    company.isActive = !company.isActive;
    this.stats.activeCompanies = this.companiesList.filter(c => c.isActive).length;
  }

  // Company features
  addBus() {
    if (!this.newBus.busNumber || !this.newBus.capacity) return;

    const id = this.busesList.length + 1;
    this.busesList.push({
      id: id,
      companyName: this.currentUser?.companyName || 'El Moussafir',
      busNumber: this.newBus.busNumber.toUpperCase(),
      capacity: this.newBus.capacity
    });

    this.newBus = { busNumber: '', capacity: 30 };
    this.stats.totalBuses = this.busesList.length;
  }

  addTrip() {
    if (!this.newTrip.departureCity || !this.newTrip.arrivalCity || !this.newTrip.departureDate || !this.newTrip.departureTime) return;

    const selectedBus = this.busesList.find(b => b.id === Number(this.newTrip.busId)) || this.busesList[0];

    this.tripsList.push({
      id: this.tripsList.length + 1,
      busNumber: selectedBus.busNumber,
      companyName: this.currentUser?.companyName || 'El Moussafir',
      departureCity: this.newTrip.departureCity,
      arrivalCity: this.newTrip.arrivalCity,
      departureDate: this.newTrip.departureDate,
      departureTime: this.newTrip.departureTime,
      price: this.newTrip.price,
      status: 'SCHEDULED'
    });

    this.newTrip = { busId: selectedBus.id, departureCity: '', arrivalCity: '', departureDate: '', departureTime: '', price: 500 };
  }

  updateTripStatus(trip: any, status: string) {
    trip.status = status;
  }

  // Ticket scan simulation
  scanTicket() {
    this.scanResult = null;
    this.scanError = '';

    if (!this.scanQrInput) {
      this.scanError = 'Veuillez saisir un code ticket ou scanner.';
      return;
    }

    // Le format attendu : TICKET-{reservationId}-TRIP{tripId}-SEAT{seatNumber}-{hash}
    // Simulation de recherche de ticket
    const parts = this.scanQrInput.split('-');
    
    if (parts.length < 5 || parts[0] !== 'TICKET') {
      this.scanError = 'Code QR invalide ou illisible. Billet rejeté.';
      return;
    }

    const tripId = Number(parts[2].replace('TRIP', ''));
    const seatNumber = Number(parts[3].replace('SEAT', ''));

    const trip = this.tripsList.find(t => t.id === tripId);
    
    if (!trip) {
      this.scanError = 'Ce trajet n\'existe plus dans le système.';
      return;
    }

    if (this.currentUser?.role === 'COMPANY' && trip.companyName !== this.currentUser?.companyName) {
      this.scanError = `Ce billet appartient à la compagnie "${trip.companyName}". Vous ne pouvez pas le valider.`;
      return;
    }

    // Succès
    this.scanResult = {
      ticketCode: this.scanQrInput,
      passengerName: 'Ahmed Ould Mohamed',
      departure: trip.departureCity,
      arrival: trip.arrivalCity,
      date: trip.departureDate,
      time: trip.departureTime,
      seat: seatNumber,
      company: trip.companyName,
      status: trip.status === 'CANCELLED' ? 'Annulé' : 'Validé - Prêt pour embarquement'
    };

    if (trip.status === 'CANCELLED') {
      this.scanError = 'ATTENTION : Ce trajet a été annulé par la compagnie.';
    }

    this.scanQrInput = '';
  }
}

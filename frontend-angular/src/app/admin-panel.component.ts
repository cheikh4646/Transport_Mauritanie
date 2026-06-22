import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './admin-panel.component.html',
  styleUrl: './admin-panel.component.css'
})
export class AdminPanelComponent implements OnInit {
  title = 'Back Office - RIM Transport';
  apiUrl = 'http://localhost:5078/api';
  
  isLoggedIn = false;
  loginForm = { email: '', password: '' };
  authError = '';
  currentUser: any = null;
  activeView = 'dashboard';

  companiesList: any[] = [];
  busesList: any[] = [];
  tripsList: any[] = [];

  CITIES = ["Nouakchott", "Nouadhibou", "Rosso", "Atar", "Zouerate", "Kiffa", "Kaédi", "Néma", "Tidjikja", "Akjoujt", "Selibaby"];

  mockCompanies = [
    { id: 1, name: 'El Moussafir', phone: '+222 45250001', email: 'contact@elmoussafir.mr', address: 'Carrefour Madrid, Nouakchott', isActive: true },
    { id: 2, name: 'Sonef Mauritanie', phone: '+222 45250002', email: 'contact@sonef.mr', address: 'Avenue Charles de Gaulle, Nouakchott', isActive: true },
    { id: 3, name: 'Sahara Trans', phone: '+222 45250003', email: 'info@saharatrans.mr', address: 'Carrefour BMD, Nouakchott', isActive: false }
  ];

  mockBuses = [
    { id: 1, companyName: 'El Moussafir', busNumber: '1234AA01', capacity: 30 },
    { id: 2, companyName: 'El Moussafir', busNumber: '5678AA01', capacity: 15 },
    { id: 3, companyName: 'Sonef Mauritanie', busNumber: '9999AB02', capacity: 50 }
  ];

  mockTrips = [
    { id: 1, busNumber: '1234AA01', companyName: 'El Moussafir', departureCity: 'Nouakchott', arrivalCity: 'Nouadhibou', departureDate: '2026-06-20', departureTime: '07:00', price: 800, status: 'SCHEDULED' },
    { id: 2, busNumber: '5678AA01', companyName: 'El Moussafir', departureCity: 'Nouakchott', arrivalCity: 'Rosso', departureDate: '2026-06-20', departureTime: '09:00', price: 300, status: 'SCHEDULED' },
    { id: 3, busNumber: '9999AB02', companyName: 'Sonef Mauritanie', departureCity: 'Nouakchott', arrivalCity: 'Atar', departureDate: '2026-06-21', departureTime: '06:30', price: 600, status: 'SCHEDULED' }
  ];

  newCompany = { name: '', phone: '', email: '', address: '' };
  newBus = { busNumber: '', capacity: 30 };
  newTrip = { busId: 1, departureCity: '', arrivalCity: '', departureDate: '', departureTime: '', price: 500 };
  
  scanQrInput = '';
  scanResult: any = null;
  scanError = '';

  stats: any = {
    totalRevenue: 0,
    totalReservations: 0,
    activeCompanies: 0,
    totalBuses: 0
  };

  reservationsList: any[] = [];
  loadingReservations: boolean = false;
  statsLoading: boolean = false;

  usersList: any[] = [];
  showAddUser: boolean = false;
  editingUser: any = null;
  userForm = { name: '', email: '', password: '', role: 'TRAVELER' };

  async ngOnInit() {
    const savedAdmin = localStorage.getItem('admin_user');
    if (savedAdmin) {
      this.currentUser = JSON.parse(savedAdmin);
      this.isLoggedIn = true;
      this.activeView = 'dashboard';
      await this.loadAllData();
    }
  }

  getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.currentUser?.token || ''}`
    };
  }

  async handleLogin() {
    this.authError = '';
    const email = this.loginForm.email.toLowerCase();
    const password = this.loginForm.password;

    try {
      const response = await fetch(`${this.apiUrl}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });
      
      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.message || data || "Identifiants de base incorrects.");
      }
      
      if (data.role === 'TRAVELER') {
        this.authError = "Accès refusé. Ce panel est réservé aux administrateurs et aux gérants de compagnies.";
        return;
      }

      this.currentUser = {
        id: data.id,
        name: data.name,
        email: data.email,
        role: data.role,
        token: data.token,
        companyName: data.role === 'COMPANY' ? 'El Moussafir' : ''
      };

      this.isLoggedIn = true;
      localStorage.setItem('admin_user', JSON.stringify(this.currentUser));
      this.setView('dashboard');
      await this.loadAllData();

    } catch (err) {
      console.warn("API hors ligne ou introuvable, basculement en mode Démo.");
      this.handleDemoLogin(email, password);
    }
  }

  handleDemoLogin(email: string, password: string) {
    if (email === 'admin@transports.gov.mr' && password === 'password123') {
      this.currentUser = { name: 'Administrateur Central (Démo)', email: email, role: 'ADMIN' };
      this.isLoggedIn = true;
    } else if (email === 'manager@elmoussafir.mr' && password === 'password123') {
      this.currentUser = { name: 'Gérant El Moussafir (Démo)', email: email, role: 'COMPANY', companyName: 'El Moussafir' };
      this.isLoggedIn = true;
    } else if (email === 'manager@sonef.mr' && password === 'password123') {
      this.currentUser = { name: 'Gérant Sonef (Démo)', email: email, role: 'COMPANY', companyName: 'Sonef Mauritanie' };
      this.isLoggedIn = true;
    } else {
      this.authError = 'Identifiants administrateur ou compagnie incorrects.';
      return;
    }
    
    localStorage.setItem('admin_user', JSON.stringify(this.currentUser));
    this.setView('dashboard');
    this.loadMockData();
  }

  handleLogout() {
    localStorage.removeItem('admin_user');
    this.isLoggedIn = false;
    this.currentUser = null;
    this.loginForm = { email: '', password: '' };
    this.companiesList = [];
    this.busesList = [];
    this.tripsList = [];
  }

  async loadAllData() {
    try {
      const [compRes, busRes, tripRes, statsRes, resaRes] = await Promise.all([
        fetch(`${this.apiUrl}/companies`, { headers: this.getHeaders() }),
        fetch(`${this.apiUrl}/buses`, { headers: this.getHeaders() }),
        fetch(`${this.apiUrl}/trips`, { headers: this.getHeaders() }),
        fetch(`${this.apiUrl}/stats/dashboard`, { headers: this.getHeaders() }),
        fetch(`${this.apiUrl}/reservations`, { headers: this.getHeaders() })
      ]);

      if (compRes.ok) this.companiesList = await compRes.json();
      if (busRes.ok) this.busesList = await busRes.json();

      if (tripRes.ok) {
        const trips = await tripRes.json();
        this.tripsList = trips.map((t: any) => ({
          id: t.id,
          busNumber: t.busNumber,
          companyName: t.companyName,
          departureCity: t.departureCity,
          arrivalCity: t.arrivalCity,
          departureDate: t.departureDate.substring(0, 10),
          departureTime: t.departureTime.substring(0, 5),
          price: t.price,
          status: t.status
        }));
      }

      if (statsRes.ok) {
        this.stats = await statsRes.json();
      }

      if (resaRes.ok) {
        this.reservationsList = await resaRes.json();
      }

      this.loadUsers();
      this.updateStats();
    } catch (e) {
      console.warn("Échec du chargement depuis l'API, chargement des mocks.", e);
      this.loadMockData();
    }
  }

  loadMockData() {
    this.companiesList = [...this.mockCompanies];
    this.busesList = [...this.mockBuses];
    this.tripsList = [...this.mockTrips];
    this.reservationsList = [];
    this.updateStats();
  }

  updateStats() {
    if (!this.stats.totalRevenue && !this.stats.totalReservations) {
      this.stats = {
        totalRevenue: this.tripsList.reduce((acc: number, t: any) => acc + (t.status === 'ARRIVED' ? t.price * 15 : t.price * 8), 12000),
        totalReservations: 380,
        activeCompanies: this.companiesList.filter((c: any) => c.isActive).length,
        totalBuses: this.busesList.length
      };
    }
  }

  setView(view: string) {
    this.activeView = view;
    this.scanResult = null;
    this.scanError = '';
  }

  async addCompany() {
    if (!this.newCompany.name || !this.newCompany.phone || !this.newCompany.email) return;
    try {
      const res = await fetch(`${this.apiUrl}/companies`, {
        method: 'POST', headers: this.getHeaders(),
        body: JSON.stringify(this.newCompany)
      });
      if (!res.ok) throw new Error();
      await this.loadAllData();
    } catch {
      const id = this.companiesList.length + 1;
      this.companiesList.push({
        id: id, name: this.newCompany.name, phone: this.newCompany.phone,
        email: this.newCompany.email, address: this.newCompany.address || 'Non spécifiée', isActive: true
      });
      this.updateStats();
    }
    this.newCompany = { name: '', phone: '', email: '', address: '' };
  }

  async toggleCompany(company: any) {
    company.isActive = !company.isActive;
    try {
      await fetch(`${this.apiUrl}/companies/${company.id}`, {
        method: 'PUT', headers: this.getHeaders(), body: JSON.stringify(company)
      });
    } catch {}
    this.updateStats();
  }

  async addBus() {
    if (!this.newBus.busNumber || !this.newBus.capacity) return;
    try {
      const res = await fetch(`${this.apiUrl}/buses`, {
        method: 'POST', headers: this.getHeaders(),
        body: JSON.stringify({ busNumber: this.newBus.busNumber.toUpperCase(), capacity: Number(this.newBus.capacity) })
      });
      if (!res.ok) throw new Error();
      await this.loadAllData();
    } catch {
      this.busesList.push({
        id: this.busesList.length + 1, companyName: this.currentUser?.companyName || 'El Moussafir',
        busNumber: this.newBus.busNumber.toUpperCase(), capacity: Number(this.newBus.capacity)
      });
      this.updateStats();
    }
    this.newBus = { busNumber: '', capacity: 30 };
  }

  async addTrip() {
    if (!this.newTrip.departureCity || !this.newTrip.arrivalCity || !this.newTrip.departureDate || !this.newTrip.departureTime) return;
    try {
      const res = await fetch(`${this.apiUrl}/trips`, {
        method: 'POST', headers: this.getHeaders(),
        body: JSON.stringify({
          busId: Number(this.newTrip.busId), departureCity: this.newTrip.departureCity,
          arrivalCity: this.newTrip.arrivalCity, departureDate: new Date(this.newTrip.departureDate).toISOString(),
          departureTime: this.newTrip.departureTime + ":00", price: Number(this.newTrip.price)
        })
      });
      if (!res.ok) throw new Error();
      await this.loadAllData();
    } catch {
      const selectedBus = this.busesList.find((b: any) => b.id === Number(this.newTrip.busId)) || this.busesList[0];
      this.tripsList.push({
        id: this.tripsList.length + 1, busNumber: selectedBus.busNumber,
        companyName: this.currentUser?.companyName || 'El Moussafir',
        departureCity: this.newTrip.departureCity, arrivalCity: this.newTrip.arrivalCity,
        departureDate: this.newTrip.departureDate, departureTime: this.newTrip.departureTime,
        price: Number(this.newTrip.price), status: 'SCHEDULED'
      });
    }
    this.newTrip = { busId: this.busesList[0]?.id || 1, departureCity: '', arrivalCity: '', departureDate: '', departureTime: '', price: 500 };
  }

  async updateTripStatus(trip: any, status: string) {
    trip.status = status;
    try {
      await fetch(`${this.apiUrl}/trips/${trip.id}/status`, {
        method: 'PUT', headers: this.getHeaders(), body: JSON.stringify(status)
      });
    } catch {}
  }

  // === USER MANAGEMENT ===

  async loadUsers() {
    try {
      const res = await fetch(`${this.apiUrl}/users`, { headers: this.getHeaders() });
      if (res.ok) this.usersList = await res.json();
    } catch {}
  }

  async addUser() {
    if (!this.userForm.name || !this.userForm.email || !this.userForm.password) return;
    try {
      const res = await fetch(`${this.apiUrl}/auth/register`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(this.userForm)
      });
      if (!res.ok) throw new Error();
      await this.loadUsers();
      this.showAddUser = false;
      this.userForm = { name: '', email: '', password: '', role: 'TRAVELER' };
    } catch { alert("Échec de la création de l'utilisateur."); }
  }

  async deleteUser(id: number) {
    if (!confirm("Supprimer cet utilisateur ? Cette action est irréversible.")) return;
    try {
      const res = await fetch(`${this.apiUrl}/users/${id}`, {
        method: 'DELETE', headers: this.getHeaders()
      });
      if (!res.ok) { const d = await res.json(); throw new Error(d.message || d); }
      await this.loadUsers();
    } catch (err: any) { alert(err.message); }
  }

  async saveUser() {
    if (!this.editingUser) return;
    const body: any = {};
    if (this.editingUser.name) body.name = this.editingUser.name;
    if (this.editingUser.email) body.email = this.editingUser.email;
    if (this.editingUser.role) body.role = this.editingUser.role;
    if (this.editingUser.newPassword) body.password = this.editingUser.newPassword;

    try {
      const res = await fetch(`${this.apiUrl}/users/${this.editingUser.id}`, {
        method: 'PUT', headers: this.getHeaders(),
        body: JSON.stringify(body)
      });
      if (!res.ok) throw new Error("Échec de la mise à jour.");
      await this.loadUsers();
      this.editingUser = null;
    } catch { alert("Erreur lors de la modification."); }
  }

  async scanTicket() {
    this.scanResult = null;
    this.scanError = '';
    if (!this.scanQrInput) {
      this.scanError = 'Veuillez saisir un code ticket ou scanner.';
      return;
    }
    try {
      const res = await fetch(`${this.apiUrl}/reservations/scan/${this.scanQrInput}`, { headers: this.getHeaders() });
      if (!res.ok) throw new Error("Code QR invalide ou illisible. Billet rejeté.");
      const data = await res.json();
      this.scanResult = {
        ticketCode: this.scanQrInput, passengerName: data.userName,
        departure: data.departureCity, arrival: data.arrivalCity,
        date: data.departureDate.substring(0, 10), time: data.departureTime.substring(0, 5),
        seat: data.seatNumber, company: data.companyName,
        status: data.status === 'PAID' ? 'Validé - Prêt pour embarquement' : 'Billet non réglé'
      };
    } catch (err: any) {
      console.warn("API Hors-ligne ou erreur, simulation locale.");
      this.scanTicketDemoFallback();
    }
  }

  scanTicketDemoFallback() {
    const parts = this.scanQrInput.split('-');
    if (parts.length < 5 || parts[0] !== 'TICKET') {
      this.scanError = 'Code QR invalide ou illisible. Billet rejeté.';
      return;
    }
    const tripId = Number(parts[2].replace('TRIP', ''));
    const seatNumber = Number(parts[3].replace('SEAT', ''));
    const trip = this.tripsList.find((t: any) => t.id === tripId);
    if (!trip) { this.scanError = 'Ce trajet n\'existe plus dans le système.'; return; }
    if (this.currentUser?.role === 'COMPANY' && trip.companyName !== this.currentUser?.companyName) {
      this.scanError = `Ce billet appartient à la compagnie "${trip.companyName}". Vous ne pouvez pas le valider.`;
      return;
    }
    this.scanResult = {
      ticketCode: this.scanQrInput, passengerName: 'Ahmed Ould Mohamed (Démo)',
      departure: trip.departureCity, arrival: trip.arrivalCity,
      date: trip.departureDate, time: trip.departureTime,
      seat: seatNumber, company: trip.companyName,
      status: trip.status === 'CANCELLED' ? 'Annulé' : 'Validé - Prêt pour embarquement'
    };
    if (trip.status === 'CANCELLED') { this.scanError = 'ATTENTION : Ce trajet a été annulé par la compagnie.'; }
    this.scanQrInput = '';
  }
}

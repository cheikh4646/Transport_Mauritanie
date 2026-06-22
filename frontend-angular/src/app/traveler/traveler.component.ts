import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TranslationService } from '../services/translation.service';
import { TranslatePipe } from '../pipes/translate.pipe';

const CITIES = [
  "Nouakchott", "Nouadhibou", "Rosso", "Atar", "Zouerate",
  "Kiffa", "Kaédi", "Néma", "Tidjikja", "Akjoujt", "Selibaby"
];

@Component({
  selector: 'app-traveler',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './traveler.component.html',
  styleUrls: ['./traveler.component.css']
})
export class TravelerComponent implements OnInit {
  apiUrl = 'http://localhost:5078/api';

  constructor(public ts: TranslationService) {}

  // View navigation
  view: string = 'home';

  // Auth
  user: any = null;
  authMode: string = 'login';
  authForm = { name: '', email: '', password: '', role: 'TRAVELER' };
  authError: string = '';

  // Search
  search = { from: '', to: '', date: '' };
  trips: any[] = [];
  loading: boolean = false;
  searchError: string = '';

  // Booking
  selectedTrip: any = null;
  selectedSeat: number | null = null;
  bookingStep: number = 1;

  // Payment
  reservation: any = null;
  paymentMethod: string = 'BANKILY';
  paymentDetails = { phone: '', pin: '' };
  paymentLoading: boolean = false;

  // Ticket
  ticket: any = null;
  qrCodeUrl: string = '';

  // My Reservations
  myReservations: any[] = [];
  loadingReservations: boolean = false;
  reservationError: string = '';
  cancelLoading: number | null = null;

  // Inline errors (replaces alert())
  bookingError: string = '';
  paymentError: string = '';

  CITIES = CITIES;
  today: string = new Date().toISOString().split('T')[0];

  ngOnInit() {
    const savedUser = localStorage.getItem('traveler_user');
    if (savedUser) {
      try {
        this.user = JSON.parse(savedUser);
      } catch {
        localStorage.removeItem('traveler_user');
      }
    }
  }

  getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.user?.token || ''}`
    };
  }

  async handleAuth(e: Event) {
    e.preventDefault();
    this.authError = '';
    const endpoint = this.authMode === 'login' ? 'login' : 'register';

    try {
      const body = this.authMode === 'login'
        ? { email: this.authForm.email, password: this.authForm.password }
        : this.authForm;

      const res = await fetch(`${this.apiUrl}/auth/${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.message || data || "Une erreur est survenue.");

      localStorage.setItem('traveler_user', JSON.stringify(data));
      this.user = data;
      this.view = 'home';
    } catch (err: any) {
      this.authError = err.message;
    }
  }

  async handleSearch(e?: Event) {
    if (e) e.preventDefault();
    if (!this.search.from || !this.search.to) {
      this.searchError = 'Veuillez renseigner les villes de départ et d\'arrivée.';
      return;
    }

    this.loading = true;
    this.searchError = '';

    try {
      let url = `${this.apiUrl}/trips?from=${this.search.from}&to=${this.search.to}`;
      if (this.search.date) url += `&date=${this.search.date}`;

      const res = await fetch(url);
      if (!res.ok) throw new Error("Impossible de récupérer les trajets.");
      this.trips = await res.json();
      this.view = 'search';
    } catch (err: any) {
      this.searchError = err.message;
    } finally {
      this.loading = false;
    }
  }

  async loadMyReservations() {
    this.loadingReservations = true;
    this.reservationError = '';
    try {
      const res = await fetch(`${this.apiUrl}/reservations`, { headers: this.getHeaders() });
      if (!res.ok) throw new Error("Impossible de charger vos réservations.");
      this.myReservations = await res.json();
    } catch (err: any) {
      this.reservationError = err.message;
    } finally {
      this.loadingReservations = false;
    }
  }

  async cancelReservation(id: number) {
    if (!confirm("Voulez-vous vraiment annuler cette réservation ?")) return;
    this.cancelLoading = id;
    try {
      const res = await fetch(`${this.apiUrl}/reservations/${id}`, {
        method: 'DELETE',
        headers: this.getHeaders()
      });
      if (!res.ok) {
        const data = await res.json();
        throw new Error(data.message || "Échec de l'annulation.");
      }
      await this.loadMyReservations();
    } catch (err: any) {
      this.reservationError = err.message;
    } finally {
      this.cancelLoading = null;
    }
  }

  async handleCreateReservation(trip: any, seatNumber: number) {
    if (!this.user) {
      this.authMode = 'login';
      this.view = 'auth';
      return;
    }

    this.bookingError = '';
    this.bookingStep = 2;
    try {
      const res = await fetch(`${this.apiUrl}/reservations`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({ tripId: trip.id, seatNumber })
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data || "Échec de la réservation.");

      this.reservation = data;
      this.bookingStep = 3;
      this.view = 'payment';
    } catch (err: any) {
      this.bookingError = err.message;
      this.bookingStep = 1;
    }
  }

  async handlePayment(e: Event) {
    e.preventDefault();
    if (!this.reservation) return;

    this.paymentLoading = true;
    this.paymentError = '';
    try {
      await new Promise(r => setTimeout(r, 2000));

      const res = await fetch(`${this.apiUrl}/reservations/${this.reservation.id}/pay`, {
        method: 'POST',
        headers: this.getHeaders(),
        body: JSON.stringify({
          reservationId: this.reservation.id,
          method: this.paymentMethod,
          amount: this.reservation.price,
          transactionReference: `TX-${this.paymentMethod}-${this.paymentDetails.phone}-${Math.random().toString(36).substring(2, 8).toUpperCase()}`
        })
      });

      const data = await res.json();
      if (!res.ok) throw new Error("Échec du paiement.");

      this.ticket = data.ticket;
      this.qrCodeUrl = `${this.apiUrl}/tickets/qr/${this.reservation.id}?t=${Date.now()}`;
      this.view = 'ticket';
    } catch (err: any) {
      this.paymentError = err.message;
    } finally {
      this.paymentLoading = false;
    }
  }

  handleLogout() {
    localStorage.removeItem('traveler_user');
    this.user = null;
    this.view = 'home';
    this.reservation = null;
    this.ticket = null;
    this.selectedTrip = null;
    this.selectedSeat = null;
  }

  getBookedSeats(trip: any): number[] {
    return trip.bookedSeats || [];
  }

  getRandomString(): string {
    return Math.random().toString(36).substring(2, 8).toUpperCase();
  }

  printTicket() {
    window.print();
  }
}

import React, { useState, useEffect } from 'react';
import { 
  Search, 
  MapPin, 
  Calendar, 
  User, 
  LogOut, 
  CreditCard, 
  QrCode, 
  Clock, 
  AlertCircle, 
  CheckCircle2, 
  ChevronRight, 
  Bus, 
  ArrowRightLeft, 
  Download,
  Info
} from 'lucide-react';

const CITIES = [
  "Nouakchott", "Nouadhibou", "Rosso", "Atar", "Zouerate", 
  "Kiffa", "Kaédi", "Néma", "Tidjikja", "Akjoujt", "Selibaby"
];

const API_URL = "http://localhost:5000/api"; // L'URL de notre API .NET Core

export default function App() {
  // Navigation & State
  const [view, setView] = useState('home'); // 'home', 'search', 'booking', 'payment', 'ticket', 'auth'
  const [user, setUser] = useState(null);
  
  // Auth state
  const [authMode, setAuthMode] = useState('login'); // 'login' or 'register'
  const [authForm, setAuthForm] = useState({ name: '', email: '', password: '', role: 'TRAVELER' });
  const [authError, setAuthError] = useState('');

  // Search state
  const [search, setSearch] = useState({ from: '', to: '', date: '' });
  const [trips, setTrips] = useState([]);
  const [loading, setLoading] = useState(false);
  const [searchError, setSearchError] = useState('');

  // Booking & Payment state
  const [selectedTrip, setSelectedTrip] = useState(null);
  const [selectedSeat, setSelectedSeat] = useState(null);
  const [reservation, setReservation] = useState(null);
  const [paymentMethod, setPaymentMethod] = useState('BANKILY');
  const [paymentDetails, setPaymentDetails] = useState({ phone: '', pin: '' });
  const [paymentLoading, setPaymentLoading] = useState(false);
  
  // Final ticket
  const [ticket, setTicket] = useState(null);

  // Load user from localStorage on mount
  useEffect(() => {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (e) {
        localStorage.removeItem('user');
      }
    }
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('user');
    setUser(null);
    setView('home');
    setReservation(null);
    setTicket(null);
    setSelectedTrip(null);
    setSelectedSeat(null);
  };

  // Auth handler
  const handleAuth = async (e) => {
    e.preventDefault();
    setAuthError('');
    const endpoint = authMode === 'login' ? 'login' : 'register';
    
    try {
      const response = await fetch(`${API_URL}/auth/${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(authMode === 'login' 
          ? { email: authForm.email, password: authForm.password }
          : authForm
        )
      });
      
      const data = await response.json();
      if (!response.ok) {
        throw new Error(data.message || data || "Une erreur est survenue.");
      }
      
      localStorage.setItem('user', JSON.stringify(data));
      setUser(data);
      setView('home');
    } catch (err) {
      setAuthError(err.message);
    }
  };

  // Search handler
  const handleSearch = async (e) => {
    if (e) e.preventDefault();
    if (!search.from || !search.to) {
      setSearchError('Veuillez renseigner les villes de départ et d\'arrivée.');
      return;
    }
    
    setLoading(true);
    setSearchError('');
    
    try {
      let url = `${API_URL}/trips?from=${search.from}&to=${search.to}`;
      if (search.date) {
        url += `&date=${search.date}`;
      }
      
      const response = await fetch(url);
      if (!response.ok) throw new Error("Impossible de récupérer les trajets.");
      const data = await response.json();
      
      setTrips(data);
      setView('search');
    } catch (err) {
      setSearchError(err.message);
    } finally {
      setLoading(false);
    }
  };

  // Create Reservation
  const handleCreateReservation = async (trip, seatNumber) => {
    if (!user) {
      setAuthMode('login');
      setView('auth');
      return;
    }

    try {
      const response = await fetch(`${API_URL}/reservations`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({ tripId: trip.id, seatNumber })
      });

      const data = await response.json();
      if (!response.ok) throw new Error(data || "Échec de la réservation.");

      setReservation(data);
      setView('payment');
    } catch (err) {
      alert(err.message);
    }
  };

  // Process payment
  const handlePayment = async (e) => {
    e.preventDefault();
    if (!reservation) return;

    setPaymentLoading(true);
    try {
      // Simulation d'attente réseau
      await new Promise(r => setTimeout(r, 2000));

      const response = await fetch(`${API_URL}/reservations/${reservation.id}/pay`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${user.token}`
        },
        body: JSON.stringify({
          reservationId: reservation.id,
          method: paymentMethod,
          amount: reservation.price,
          transactionReference: `TX-${paymentMethod}-${paymentDetails.phone}-${new RandomString()}`
        })
      });

      const data = await response.json();
      if (!response.ok) throw new Error("Échec du paiement.");

      setTicket(data.ticket);
      setView('ticket');
    } catch (err) {
      alert(err.message);
    } finally {
      setPaymentLoading(false);
    }
  };

  const RandomString = () => {
    return Math.random().toString(36).substring(2, 8).toUpperCase();
  };

  return (
    <div className="min-h-screen flex flex-col">
      {/* Navigation */}
      <header className="glass-card sticky top-0 z-50 border-b border-white/5 bg-[#0b0f19]/80 backdrop-blur-md">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-20 flex items-center justify-between">
          <div className="flex items-center gap-3 cursor-pointer" onClick={() => setView('home')}>
            <div className="p-2.5 bg-emerald-600/10 rounded-xl border border-emerald-500/20">
              <Bus className="h-6 w-6 text-emerald-500" />
            </div>
            <div>
              <h1 className="font-extrabold text-xl tracking-tight text-white flex items-center gap-1.5">
                RIM <span className="text-emerald-500">Transport</span>
              </h1>
              <p className="text-[10px] text-gray-500 font-medium tracking-widest uppercase">Mauritanie</p>
            </div>
          </div>

          <nav className="flex items-center gap-6">
            <button onClick={() => setView('home')} className="text-sm font-semibold text-gray-300 hover:text-white transition-colors">
              Accueil
            </button>
            {user ? (
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2 px-3.5 py-1.5 bg-white/5 rounded-full border border-white/5">
                  <User className="h-4 w-4 text-emerald-400" />
                  <span className="text-sm font-medium text-gray-200">{user.name}</span>
                </div>
                <button onClick={handleLogout} className="p-2 bg-red-500/10 text-red-400 hover:bg-red-500/20 rounded-xl transition-all border border-red-500/20">
                  <LogOut className="h-4 w-4" />
                </button>
              </div>
            ) : (
              <button 
                onClick={() => { setAuthMode('login'); setView('auth'); }} 
                className="btn btn-primary py-2 px-5 text-sm"
              >
                Se connecter
              </button>
            )}
          </nav>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-grow max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 w-full">
        {view === 'home' && (
          <div className="max-w-3xl mx-auto space-y-12">
            <div className="text-center space-y-4 py-8">
              <span className="px-3.5 py-1.5 bg-emerald-600/10 text-emerald-400 text-xs font-semibold tracking-wider rounded-full border border-emerald-500/20">
                PFE - Plateforme Nationale du Transport Interurbain
              </span>
              <h2 className="text-4xl sm:text-5xl font-extrabold text-white tracking-tight">
                Voyagez partout en <span className="bg-gradient-to-r from-emerald-400 to-amber-400 bg-clip-text text-transparent">Mauritanie</span>
              </h2>
              <p className="text-gray-400 text-lg max-w-xl mx-auto">
                Réservez vos places à l'avance auprès des plus grandes compagnies de transport et payez en ligne avec Bankily ou Masrify.
              </p>
            </div>

            {/* Search Engine Form */}
            <form onSubmit={handleSearch} className="glass-card p-6 sm:p-8 space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="form-group m-0">
                  <label className="form-label flex items-center gap-2"><MapPin className="h-4 w-4 text-emerald-500" /> Ville de départ</label>
                  <select 
                    className="form-input w-full bg-[#131a2e]"
                    value={search.from}
                    onChange={e => setSearch({ ...search, from: e.target.value })}
                  >
                    <option value="">Sélectionner...</option>
                    {CITIES.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>

                <div className="form-group m-0">
                  <label className="form-label flex items-center gap-2"><MapPin className="h-4 w-4 text-amber-500" /> Ville d'arrivée</label>
                  <select 
                    className="form-input w-full bg-[#131a2e]"
                    value={search.to}
                    onChange={e => setSearch({ ...search, to: e.target.value })}
                  >
                    <option value="">Sélectionner...</option>
                    {CITIES.map(c => <option key={c} value={c}>{c}</option>)}
                  </select>
                </div>

                <div className="form-group m-0">
                  <label className="form-label flex items-center gap-2"><Calendar className="h-4 w-4 text-emerald-500" /> Date de départ</label>
                  <input 
                    type="date" 
                    className="form-input w-full"
                    value={search.date}
                    onChange={e => setSearch({ ...search, date: e.target.value })}
                  />
                </div>
              </div>

              {searchError && (
                <div className="flex items-center gap-2.5 p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-400 text-sm">
                  <AlertCircle className="h-5 w-5 shrink-0" />
                  <span>{searchError}</span>
                </div>
              )}

              <button 
                type="submit" 
                disabled={loading}
                className="btn btn-accent w-full py-4 text-base font-bold pulse-accent"
              >
                {loading ? 'Recherche en cours...' : 'Rechercher des trajets'}
              </button>
            </form>
          </div>
        )}

        {view === 'search' && (
          <div className="space-y-6 max-w-4xl mx-auto">
            <div className="flex items-center justify-between border-b border-white/5 pb-4">
              <div>
                <h3 className="text-xl font-bold text-white flex items-center gap-3">
                  <span>{search.from}</span>
                  <ChevronRight className="h-4 w-4 text-gray-500" />
                  <span>{search.to}</span>
                </h3>
                <p className="text-sm text-gray-400 mt-1">Trajets trouvés pour votre recherche</p>
              </div>
              <button onClick={() => setView('home')} className="btn btn-secondary py-2 px-4 text-xs font-semibold">
                Modifier la recherche
              </button>
            </div>

            {trips.length === 0 ? (
              <div className="glass-card p-12 text-center space-y-4">
                <Info className="h-10 w-10 text-amber-500 mx-auto" />
                <h4 className="text-lg font-bold text-white">Aucun trajet trouvé</h4>
                <p className="text-gray-400 max-w-md mx-auto">
                  Aucun bus n'est programmé sur cette ligne pour la date sélectionnée. Veuillez essayer une autre date ou modifier vos villes.
                </p>
              </div>
            ) : (
              <div className="grid gap-6">
                {trips.map(trip => (
                  <div key={trip.id} className="glass-card p-6 flex flex-col md:flex-row md:items-center justify-between gap-6">
                    <div className="space-y-3">
                      <div className="flex items-center gap-2">
                        <span className="px-2.5 py-1 bg-emerald-600/15 text-emerald-400 text-[10px] font-bold uppercase rounded border border-emerald-500/10">
                          {trip.companyName}
                        </span>
                        <span className="text-xs text-gray-500 flex items-center gap-1">
                          <Bus className="h-3 w-3" /> Bus N° {trip.busNumber}
                        </span>
                      </div>
                      <div className="flex items-center gap-6">
                        <div>
                          <p className="text-lg font-bold text-white">{trip.departureTime.substring(0, 5)}</p>
                          <p className="text-xs text-gray-400">{trip.departureCity}</p>
                        </div>
                        <div className="flex flex-col items-center">
                          <div className="w-12 h-0.5 bg-gray-700 relative">
                            <div className="absolute right-0 top-1/2 -translate-y-1/2 w-1.5 h-1.5 rounded-full bg-emerald-500"></div>
                          </div>
                        </div>
                        <div>
                          <p className="text-lg font-bold text-white">Arrivée</p>
                          <p className="text-xs text-gray-400">{trip.arrivalCity}</p>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center justify-between md:justify-end gap-6 border-t md:border-none border-white/5 pt-4 md:pt-0">
                      <div className="text-left md:text-right">
                        <p className="text-2xl font-black text-amber-400">{trip.price} MRU</p>
                        <p className="text-xs text-gray-400">{trip.busCapacity - trip.bookedSeats.length} places libres</p>
                      </div>
                      <button 
                        onClick={() => { setSelectedTrip(trip); setView('booking'); }}
                        className="btn btn-primary py-2.5 px-6 text-sm"
                      >
                        Réserver
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {view === 'booking' && selectedTrip && (
          <div className="max-w-4xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Bus Seating Map */}
            <div className="lg:col-span-2 glass-card p-6 sm:p-8 space-y-6">
              <div>
                <h3 className="text-lg font-bold text-white">Sélection du siège</h3>
                <p className="text-xs text-gray-400">Cliquez sur un siège vert disponible pour le réserver</p>
              </div>

              {/* Seating Grid */}
              <div className="border border-white/5 bg-[#090d16] rounded-xl p-8 max-w-sm mx-auto">
                {/* Front (Driver) */}
                <div className="flex justify-between items-center border-b border-white/5 pb-4 mb-6">
                  <span className="text-[10px] text-gray-500 font-bold tracking-widest uppercase">Avant du Bus</span>
                  <div className="w-8 h-8 rounded bg-gray-800 flex items-center justify-center text-[10px] font-bold text-gray-500">
                    Chauffeur
                  </div>
                </div>

                <div className="grid grid-cols-4 gap-4 justify-items-center">
                  {Array.from({ length: selectedTrip.busCapacity }).map((_, i) => {
                    const seatNum = i + 1;
                    const isBooked = selectedTrip.bookedSeats.includes(seatNum);
                    const isSelected = selectedSeat === seatNum;

                    return (
                      <button
                        key={seatNum}
                        disabled={isBooked}
                        onClick={() => setSelectedSeat(seatNum)}
                        className={`w-10 h-10 rounded-lg flex items-center justify-center text-xs font-bold transition-all
                          ${isBooked ? 'bg-red-500/10 text-red-500/30 border border-red-500/10 cursor-not-allowed' : 
                            isSelected ? 'bg-amber-500 text-bg-primary font-black scale-110 shadow-lg shadow-amber-500/20' : 
                            'bg-emerald-600/20 text-emerald-400 border border-emerald-500/20 hover:bg-emerald-600/40'}`}
                      >
                        {seatNum}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Legend */}
              <div className="flex items-center justify-center gap-6 text-xs font-semibold pt-4">
                <div className="flex items-center gap-2"><div className="w-4 h-4 rounded bg-emerald-600/20 border border-emerald-500/20"></div> Disponible</div>
                <div className="flex items-center gap-2"><div className="w-4 h-4 rounded bg-red-500/10 border border-red-500/10"></div> Occupé</div>
                <div className="flex items-center gap-2"><div className="w-4 h-4 rounded bg-amber-500"></div> Votre choix</div>
              </div>
            </div>

            {/* Selection Summary */}
            <div className="glass-card p-6 h-fit space-y-6">
              <h3 className="font-bold text-lg text-white">Résumé du Trajet</h3>
              <div className="space-y-4 border-b border-white/5 pb-4 text-sm text-gray-400">
                <div className="flex justify-between"><span>Compagnie</span><strong className="text-white">{selectedTrip.companyName}</strong></div>
                <div className="flex justify-between"><span>Bus</span><strong className="text-white">{selectedTrip.busNumber}</strong></div>
                <div className="flex justify-between"><span>Itinéraire</span><strong className="text-white">{selectedTrip.departureCity} &rarr; {selectedTrip.arrivalCity}</strong></div>
                <div className="flex justify-between"><span>Date</span><strong className="text-white">{new Date(selectedTrip.departureDate).toLocaleDateString()}</strong></div>
                <div className="flex justify-between"><span>Heure</span><strong className="text-white">{selectedTrip.departureTime.substring(0,5)}</strong></div>
                <div className="flex justify-between"><span>Siège sélectionné</span><strong className="text-amber-400 font-extrabold">{selectedSeat || 'Aucun'}</strong></div>
              </div>

              <div className="flex justify-between items-end">
                <span className="text-xs text-gray-400 font-semibold">Total</span>
                <span className="text-2xl font-black text-white">{selectedTrip.price} MRU</span>
              </div>

              <button
                disabled={!selectedSeat}
                onClick={() => handleCreateReservation(selectedTrip, selectedSeat)}
                className="btn btn-primary w-full py-3"
              >
                Confirmer le choix & Payer
              </button>
            </div>
          </div>
        )}

        {view === 'payment' && reservation && (
          <div className="max-w-md mx-auto glass-card p-6 sm:p-8 space-y-6">
            <div className="text-center space-y-2">
              <h3 className="text-xl font-bold text-white">Paiement Mobile sécurisé</h3>
              <p className="text-xs text-gray-400">Veuillez sélectionner votre opérateur de paiement mauritanien</p>
            </div>

            {/* Operator Selection */}
            <div className="grid grid-cols-2 gap-4">
              <button 
                onClick={() => setPaymentMethod('BANKILY')}
                className={`p-4 rounded-xl border font-bold flex flex-col items-center gap-2 transition-all
                  ${paymentMethod === 'BANKILY' ? 'bg-emerald-600/10 border-emerald-500 text-emerald-400 scale-[1.02]' : 'bg-[#131a2e] border-white/5 text-gray-400 hover:border-white/10'}`}
              >
                <div className="w-8 h-8 rounded-full bg-emerald-500 flex items-center justify-center text-bg-primary text-xs font-black">B</div>
                Bankily
              </button>

              <button 
                onClick={() => setPaymentMethod('MASRIFY')}
                className={`p-4 rounded-xl border font-bold flex flex-col items-center gap-2 transition-all
                  ${paymentMethod === 'MASRIFY' ? 'bg-amber-600/10 border-amber-500 text-amber-400 scale-[1.02]' : 'bg-[#131a2e] border-white/5 text-gray-400 hover:border-white/10'}`}
              >
                <div className="w-8 h-8 rounded-full bg-amber-500 flex items-center justify-center text-bg-primary text-xs font-black">M</div>
                Masrify
              </button>
            </div>

            <form onSubmit={handlePayment} className="space-y-4">
              <div className="form-group">
                <label className="form-label">Numéro de téléphone</label>
                <input 
                  type="tel" 
                  placeholder="Ex: 44123456" 
                  required
                  className="form-input" 
                  value={paymentDetails.phone}
                  onChange={e => setPaymentDetails({ ...paymentDetails, phone: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Code PIN de transaction</label>
                <input 
                  type="password" 
                  placeholder="••••" 
                  required
                  maxLength={4}
                  className="form-input"
                  value={paymentDetails.pin}
                  onChange={e => setPaymentDetails({ ...paymentDetails, pin: e.target.value })}
                />
              </div>

              <div className="bg-[#131a2e] p-4 rounded-xl border border-white/5 flex justify-between items-center text-sm">
                <span className="text-gray-400 font-semibold">Montant à régler :</span>
                <span className="font-extrabold text-white">{reservation.price} MRU</span>
              </div>

              <button 
                type="submit" 
                disabled={paymentLoading}
                className="btn btn-accent w-full py-4 font-bold"
              >
                {paymentLoading ? 'Validation en cours...' : `Payer via ${paymentMethod}`}
              </button>
            </form>
          </div>
        )}

        {view === 'ticket' && ticket && reservation && (
          <div className="max-w-lg mx-auto space-y-6 animate-fade-in">
            <div className="text-center space-y-2">
              <div className="inline-flex p-3 bg-emerald-500/10 rounded-full border border-emerald-500/20 text-emerald-400 mb-2">
                <CheckCircle2 className="h-8 w-8" />
              </div>
              <h3 className="text-2xl font-extrabold text-white">Félicitations, voyage confirmé !</h3>
              <p className="text-sm text-gray-400">Votre billet électronique a été généré et envoyé par e-mail.</p>
            </div>

            {/* E-Ticket Display */}
            <div className="glass-card overflow-hidden border-emerald-500/20">
              <div className="bg-gradient-to-r from-emerald-600 to-emerald-800 p-6 text-white text-center space-y-1">
                <h4 className="font-extrabold text-lg tracking-wider">BILLET DE TRANSPORT</h4>
                <p className="text-[10px] tracking-widest text-emerald-200 font-semibold uppercase">{reservation.companyName}</p>
              </div>

              <div className="p-6 space-y-6">
                <div className="grid grid-cols-2 gap-4 text-sm border-b border-white/5 pb-4">
                  <div><p className="text-xs text-gray-500 font-semibold">Passager</p><p className="font-bold text-white">{reservation.userName}</p></div>
                  <div><p className="text-xs text-gray-500 font-semibold">Référence Billet</p><p className="font-mono font-bold text-white">{ticket.qrCode.split('-')[4] || ticket.qrCode}</p></div>
                </div>

                <div className="flex justify-between items-center border-b border-white/5 pb-4">
                  <div>
                    <h5 className="text-lg font-black text-white">{reservation.departureCity}</h5>
                    <p className="text-xs text-gray-500 font-semibold">{new Date(reservation.departureDate).toLocaleDateString()}</p>
                    <p className="text-xs text-gray-400 flex items-center gap-1 mt-1"><Clock className="h-3 w-3" /> {reservation.departureTime.substring(0,5)}</p>
                  </div>
                  <div className="p-2.5 bg-white/5 rounded-xl border border-white/5">
                    <ChevronRight className="h-5 w-5 text-emerald-400" />
                  </div>
                  <div className="text-right">
                    <h5 className="text-lg font-black text-white">{reservation.arrivalCity}</h5>
                    <p className="text-xs text-gray-500 font-semibold">Bus N° {reservation.seatNumber}</p>
                    <p className="text-xs text-amber-400 font-extrabold mt-1">Siège N° {reservation.seatNumber}</p>
                  </div>
                </div>

                {/* QR Code container */}
                <div className="flex flex-col items-center gap-2 pt-2">
                  <div className="bg-white p-3.5 rounded-xl">
                    {/* Générer un QR code purement SVG pour la démo autonome */}
                    <svg className="w-32 h-32" viewBox="0 0 100 100">
                      <rect width="100" height="100" fill="white" />
                      {/* Dessin SVG simulé d'un QR code */}
                      <path d="M10 10h20v20h-20zM15 15h10v10h-10z M70 10h20v20h-20zM75 15h10v10h-10z M10 70h20v20h-20zM15 75h10v10h-10z" fill="black" />
                      <path d="M40 10h10v10h-10z M55 15h10v15h-10z M45 35h15v10h-15z M25 45h10v10h-10z M70 45h15v15h-15z M10 40h15v10h-15z M75 75h15v15h-15z M45 75h15v15h-15z M40 55h20v10h-20z" fill="black" />
                    </svg>
                  </div>
                  <span className="text-[10px] font-mono text-gray-500 font-bold uppercase tracking-widest">{ticket.qrCode}</span>
                </div>
              </div>
            </div>

            <button 
              onClick={() => window.print()}
              className="btn btn-secondary w-full py-3"
            >
              <Download className="h-4 w-4" /> Imprimer le Billet
            </button>
          </div>
        )}

        {view === 'auth' && (
          <div className="max-w-md mx-auto glass-card p-6 sm:p-8 space-y-6">
            <div className="text-center space-y-2">
              <h3 className="text-2xl font-extrabold text-white">
                {authMode === 'login' ? 'Connexion Voyageur' : 'Inscription Voyageur'}
              </h3>
              <p className="text-xs text-gray-400">
                {authMode === 'login' ? 'Saisissez vos identifiants pour continuer' : 'Remplissez le formulaire ci-dessous'}
              </p>
            </div>

            <form onSubmit={handleAuth} className="space-y-4">
              {authError && (
                <div className="flex items-center gap-2.5 p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-400 text-sm">
                  <AlertCircle className="h-5 w-5 shrink-0" />
                  <span>{authError}</span>
                </div>
              )}

              {authMode === 'register' && (
                <div className="form-group">
                  <label className="form-label">Nom complet</label>
                  <input 
                    type="text" 
                    required 
                    placeholder="Ex: Ahmed Ould Mohamed"
                    className="form-input" 
                    value={authForm.name}
                    onChange={e => setAuthForm({ ...authForm, name: e.target.value })}
                  />
                </div>
              )}

              <div className="form-group">
                <label className="form-label">Adresse e-mail</label>
                <input 
                  type="email" 
                  required 
                  placeholder="Ex: ahmed@gmail.com"
                  className="form-input"
                  value={authForm.email}
                  onChange={e => setAuthForm({ ...authForm, email: e.target.value })}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Mot de passe</label>
                <input 
                  type="password" 
                  required 
                  placeholder="Min. 6 caractères"
                  className="form-input"
                  value={authForm.password}
                  onChange={e => setAuthForm({ ...authForm, password: e.target.value })}
                />
              </div>

              <button type="submit" className="btn btn-primary w-full py-3.5 mt-2">
                {authMode === 'login' ? 'Se connecter' : 'Créer un compte'}
              </button>
            </form>

            <div className="text-center text-sm">
              <button 
                onClick={() => setAuthMode(authMode === 'login' ? 'register' : 'login')}
                className="text-emerald-400 hover:text-emerald-300 font-semibold"
              >
                {authMode === 'login' ? 'Pas encore de compte ? S\'inscrire' : 'Déjà un compte ? Se connecter'}
              </button>
            </div>
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="border-t border-white/5 bg-[#070b13] py-8 text-center text-xs text-gray-500 font-medium tracking-wide">
        &copy; {new Date().getFullYear()} RIM Transport - Projet de Fin d'Études de Gestion du Transport Interurbain en Mauritanie.
      </footer>
    </div>
  );
}

# Cahier des Charges - Plateforme de Gestion du Transport Interurbain en Mauritanie

## 1. Introduction & Présentation du Projet

Ce projet consiste en la conception et le développement d'une plateforme web centralisée pour moderniser et digitaliser la gestion du transport de passagers interurbain en Mauritanie. La plateforme met en relation :
- Les **compagnies de transport** (ex: El Moussafir, Sonef, Sahara Trans, etc.) qui gèrent leur flotte, leurs trajets et leurs ventes.
- Les **voyageurs** (citoyens mauritaniens et visiteurs) qui recherchent, réservent et paient leurs billets en ligne.
- L'**administrateur de la plateforme** (autorité de régulation ou gestionnaire central) qui supervise l'ensemble du système.

---

## 2. Contexte et Problématique

Actuellement, en Mauritanie, la réservation de trajets interurbains s'effectue principalement de manière manuelle ou par téléphone. Les voyageurs doivent se déplacer dans les gares routières pour acheter leur billet, ce qui engendre :
- Une perte de temps significative et des déplacements superflus.
- Une absence de visibilité en temps réel sur la disponibilité des sièges.
- Des risques d'erreurs (doubles réservations, pertes de registres).
- Une traçabilité financière limitée pour les compagnies.
- L'absence de statistiques consolidées pour l'administration nationale des transports.

---

## 3. Objectifs de la Solution

- **Centralisation :** Regrouper les offres de toutes les compagnies de transport interurbain agréées sur un portail unique.
- **Automatisation :** Permettre la réservation automatique des sièges avec choix sur plan de bus et génération immédiate de billets électroniques.
- **Paiement Mobile :** Intégrer les moyens de paiement locaux très populaires en Mauritanie (Bankily, Masrify, Bimie, Sadad, etc.) en plus des cartes bancaires.
- **Sécurité et Traçabilité :** Garantir la sécurité des données utilisateurs et des transactions financières, tout en luttant contre la fraude grâce à des billets munis de QR Codes scannables à l'embarquement.
- **Statistiques :** Fournir des outils de reporting en temps réel pour optimiser le taux de remplissage des bus et piloter l'activité.

---

## 4. Analyse des Acteurs (Parties Prenantes)

### 4.1. Voyageur (Client)
- **Rôle :** Recherche et réserve des trajets, sélectionne son siège, effectue le paiement et télécharge son billet électronique.
- **Besoins :** Interface mobile-friendly, recherche rapide, paiement instantané par mobile banking, historique de réservations, réception du billet par Email/SMS.

### 4.2. Compagnie de Transport (Opérateur)
- **Rôle :** Gère ses propres bus, planifie ses trajets (horaires, escales, tarifs) et supervise ses réservations.
- **Besoins :** Tableau de bord de suivi des ventes, gestion des chauffeurs et des véhicules, validation des billets à l'embarquement via scan QR Code.

### 4.3. Administrateur Général (Régulateur)
- **Rôle :** Contrôle et audite la plateforme. Valide et approuve l'adhésion des nouvelles compagnies de transport.
- **Besoins :** Statistiques globales (flux de voyageurs par ville, parts de marché par compagnie), gestion des utilisateurs et rôles, gestion des tarifs de base ou commissions.

---

## 5. Besoins Fonctionnels Détaillés

### 5.1. Authentification & Sécurité
- Inscription et connexion (email/mot de passe).
- Authentification sécurisée par jeton JWT.
- Profils et droits d'accès stricts (Contrôle d'accès basé sur les rôles - RBAC).
- Récupération de mot de passe sécurisée.

### 5.2. Gestion des Compagnies (Admin)
- Création, modification, blocage et suppression de compagnies.
- Suivi des performances et du volume de transactions de chaque compagnie.

### 5.3. Gestion du Parc de Bus (Compagnie)
- Enregistrement des bus avec numéro d'immatriculation, type, et capacité (ex: bus de 15 places, 30 places, 50 places).
- Configuration dynamique du plan des sièges (numérotation).

### 5.4. Gestion des Trajets (Compagnie)
- Création de lignes interurbaines (Ville de départ, Ville d'arrivée, distance, prix).
- Planification des trajets : association d'un bus, d'une date de départ et d'une heure précise.
- Mise à jour du statut des trajets (Planifié, En cours, Arrivé, Annulé).

### 5.5. Recherche & Réservation (Voyageur)
- Formulaire de recherche multicritères : ville de départ, ville d'arrivée, date de voyage.
- Visualisation des bus disponibles, des horaires et des prix.
- Sélection interactive du ou des sièges sur le plan graphique du bus (siège libre/occupé).
- Saisie des informations des passagers.

### 5.6. Module de Paiement
- Intégration simulée (ou réelle via API) des solutions de paiement mobile mauritaniennes :
  - **Bankily** (Banque Populaire de Mauritanie)
  - **Masrify** (Banque Mauritanienne du Commerce International)
  - **Bimie** (Banque Islamique de Mauritanie)
- Validation automatique du paiement pour confirmer la réservation.

### 5.7. Génération de Billets et QR Codes
- Génération d'un billet électronique unique (format PDF/HTML).
- Intégration d'un QR Code sécurisé contenant les informations de réservation (ID Réservation, Nom, Trajet, Date, Siège).
- Envoi automatique du billet par email.

### 5.8. Reporting et Tableaux de Bord (Dashboards)
- **Pour l'administrateur :** Graphiques financiers, nombre total de réservations, répartition géographique des trajets les plus fréquentés (ex: Nouakchott - Nouadhibou, Nouakchott - Rosso, Nouakchott - Atar).
- **Pour la compagnie :** Chiffre d'affaires par mois/semaine, taux d'occupation moyen des bus, liste des passagers d'un trajet spécifique pour impression du manifeste de voyage.

---

## 6. Besoins Non Fonctionnels

- **Sécurité :**
  - Chiffrement des mots de passe des utilisateurs en base de données (hachage sécurisé BCrypt ou similaire).
  - Protection contre les failles courantes (injections SQL, failles XSS, CSRF).
  - HTTPS obligatoire pour toutes les communications.
- **Performance & Disponibilité :**
  - Temps de réponse de l'API inférieur à 200ms pour les requêtes courantes.
  - Taux de disponibilité cible de 99.9%.
  - Gestion des accès concurrents pour éviter la double réservation d'un même siège au même instant.
- **Ergonomie & Accessibilité :**
  - Interface responsive (Mobile First pour le Portail Voyageur).
  - Support multilingue (Arabe, Français).
  - Design premium et moderne avec micro-animations et transitions douces.

---

## 7. Architecture Technique Retenue

- **Frontend Voyageur :** React (Vite, CSS modulaire, gestion d'état réactive).
- **Frontend Administrateur :** Angular (Routage typé, Services, RxJS, Modules pour un code maintenable).
- **Backend Principal :** ASP.NET Core Web API (Entity Framework Core, Clean Architecture / Repository pattern, JWT).
- **Backend Services :** Laravel API (Moteur de notifications, génération PDF, scripts de reporting).
- **Base de données :** MySQL.

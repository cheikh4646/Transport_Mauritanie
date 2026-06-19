# Conception UML - Plateforme de Gestion du Transport Interurbain en Mauritanie

Ce document présente la modélisation UML complète du système de gestion des transports.

---

## 1. Diagramme de Cas d'Utilisation (Use Case Diagram)

Ce diagramme décrit les interactions entre les différents acteurs (Voyageur, Compagnie, Administrateur) et le système.

```mermaid
leftToRightDirection
actor Voyageur
actor "Compagnie de Transport" as Compagnie
actor Administrateur

rectangle "Plateforme de Transport Interurbain" {
  usecase "S'authentifier (Connexion/Inscription)" as UC_Auth
  
  usecase "Rechercher un trajet" as UC_Search
  usecase "Sélectionner un siège" as UC_Seat
  usecase "Réserver et Payer (Bankily/Masrify/CB)" as UC_Pay
  usecase "Télécharger le billet QR Code" as UC_Ticket
  
  usecase "Gérer la flotte de bus" as UC_Bus
  usecase "Planifier des trajets" as UC_Trip
  usecase "Consulter les réservations & Passagers" as UC_ViewRes
  usecase "Valider un billet (Scan QR Code)" as UC_Scan
  
  usecase "Gérer les comptes compagnies" as UC_ManageComp
  usecase "Consulter les statistiques globales" as UC_Stats
}

Voyageur --> UC_Auth
Voyageur --> UC_Search
Voyageur --> UC_Seat
Voyageur --> UC_Pay
Voyageur --> UC_Ticket

Compagnie --> UC_Auth
Compagnie --> UC_Bus
Compagnie --> UC_Trip
Compagnie --> UC_ViewRes
Compagnie --> UC_Scan

Administrateur --> UC_Auth
Administrateur --> UC_ManageComp
Administrateur --> UC_Stats
```

---

## 2. Diagramme de Classes (Class Diagram)

Ce diagramme définit la structure des données, les attributs des entités et leurs relations/cardinalités.

```mermaid
classDiagram
    class User {
        +int id
        +string name
        +string email
        +string password
        +string role
        +register()
        +login()
    }

    class Company {
        +int id
        +string name
        +string phone
        +string email
        +string address
        +create()
        +update()
    }

    class Bus {
        +int id
        +int company_id
        +string bus_number
        +int capacity
        +create()
        +getAvailableSeats(trip_id)
    }

    class Trip {
        +int id
        +int bus_id
        +string departure_city
        +string arrival_city
        +date departure_date
        +time departure_time
        +decimal price
        +create()
        +search(from, to, date)
    }

    class Reservation {
        +int id
        +int user_id
        +int trip_id
        +int seat_number
        +string status
        +create()
        +cancel()
    }

    class Payment {
        +int id
        +int reservation_id
        +decimal amount
        +string method
        +timestamp payment_date
        +process()
    }

    class Ticket {
        +int id
        +int reservation_id
        +string qr_code
        +string pdf_path
        +generate()
    }

    User "1" -- "0..*" Reservation : effectue
    Company "1" -- "0..*" Bus : possède
    Bus "1" -- "0..*" Trip : est affecté à
    Trip "1" -- "0..*" Reservation : contient
    Reservation "1" -- "1" Payment : donne lieu à
    Reservation "1" -- "1" Ticket : génère
```

---

## 3. Diagramme de Séquence (Sequence Diagram)

Ce diagramme montre le flux d'interactions lors du processus de recherche, sélection de siège, réservation, paiement et génération de billet.

```mermaid
sequenceDiagram
    autonumber
    actor V as Voyageur
    participant R as Frontend React
    participant API as API ASP.NET Core (Core)
    participant DB as Base de données MySQL
    participant L as API Laravel (Services)

    V->>R: Renseigne trajet (Départ, Arrivée, Date)
    R->>API: GET /api/trips?from=...&to=...&date=...
    API->>DB: Rechercher trajets correspondants
    DB-->>API: Liste des trajets
    API-->>R: Liste des trajets avec places disponibles
    R-->>V: Affiche les trajets

    V->>R: Choisit un trajet & sélectionne un siège
    R->>API: POST /api/reservations (trip_id, seat_number) [JWT]
    API->>DB: Vérifier si le siège est libre
    alt Siège occupé
        DB-->>API: Indisponible
        API-->>R: Erreur : Siège déjà réservé
        R-->>V: Affiche message d'erreur
    else Siège libre
        API->>DB: Insérer Réservation (Status = En attente)
        DB-->>API: Réservation créée (ID)
        API-->>R: Réservation créée + Demande de paiement
    end

    V->>R: Choisit moyen de paiement (Bankily/Masrify) et valide
    R->>API: POST /api/payments (reservation_id, method, amount)
    API->>API: Simuler la validation du paiement mobile
    API->>DB: Mettre à jour Réservation (Status = Payée) et créer Paiement
    DB-->>API: Confirmé

    API->>L: Déclencher génération billet (reservation_id)
    Note over L: Génère le QR Code et le PDF du Billet
    L->>DB: Insérer Ticket (qr_code, pdf_path)
    DB-->>L: Enregistré
    L-->>API: Ticket généré avec succès
    
    API-->>R: Retourne les détails du Ticket + PDF + QR Code
    R-->>V: Affiche le billet avec QR Code et bouton Télécharger
```

---

## 4. Diagramme d'Activités (Activity Diagram)

Ce diagramme détaille le flux d'activités pour réserver et acheter un billet.

```mermaid
stateDiagram-v2
    [*] --> RechercheTrajet
    RechercheTrajet --> AfficherResultats
    AfficherResultats --> ChoisirTrajet : Trajet trouvé
    AfficherResultats --> [*] : Aucun trajet trouvé

    ChoisirTrajet --> VisualiserSieges
    VisualiserSieges --> SelectionnerSiege
    SelectionnerSiege --> VerifierDisponibilite
    
    state VerifierDisponibilite <<choice>>
    VerifierDisponibilite --> AfficherErreur : Siège déjà pris
    AfficherErreur --> VisualiserSieges : Choisir un autre siège
    VerifierDisponibilite --> CreerReservationTemporaire : Siège libre

    CreerReservationTemporaire --> ChoixPaiement
    
    state ChoixPaiement {
        [*] --> ModePaiement
        ModePaiement --> Bankily
        ModePaiement --> Masrify
        ModePaiement --> CarteBancaire
    }
    
    ChoixPaiement --> ExecuterPaiement
    ExecuterPaiement --> TraitementPaiement
    
    state TraitementPaiement <<choice>>
    TraitementPaiement --> PaiementEchoue : Erreur solde/transaction
    PaiementEchoue --> ChoixPaiement : Recommencer le paiement
    TraitementPaiement --> PaiementValide : Succès
    
    PaiementValide --> ValiderReservation
    ValiderReservation --> GenererQRCodeEtPDF
    GenererQRCodeEtPDF --> EnvoyerConfirmationEmail
    EnvoyerConfirmationEmail --> AfficherBilletElectronique
    AfficherBilletElectronique --> [*]
```

---

## 5. Diagramme de Déploiement (Deployment Diagram)

Ce diagramme montre la topologie physique du système et la répartition des applications sur l'infrastructure d'hébergement.

```mermaid
flowchart TD
    subgraph Client_Devices ["Équipements Clients"]
        Voyageur_Browser["Navigateur Voyageur (React App)"]
        Admin_Browser["Navigateur Admin & Compagnie (Angular App)"]
    end

    subgraph Hosting_Cloud ["Serveur d'Hébergement Web / Cloud"]
        subgraph Web_Server ["Serveur Web HTTP (Nginx / IIS)"]
            React_Build["Fichiers Statiques React (Portail)"]
            Angular_Build["Fichiers Statiques Angular (Back Office)"]
        end

        subgraph App_Server_DotNet ["Serveur API Core (.NET Runtime)"]
            DotNet_API["API ASP.NET Core (Kestrel / IIS)"]
        end

        subgraph App_Server_Laravel ["Serveur Services (PHP Runtime)"]
            Laravel_API["API Laravel (Nginx + PHP-FPM)"]
        end

        subgraph DB_Server ["Serveur de Base de Données"]
            MySQL_DB[("Base de Données MySQL")]
        end
    end

    Voyageur_Browser -- HTTPS / JSON --> React_Build
    Admin_Browser -- HTTPS / JSON --> Angular_Build

    Voyageur_Browser -- REST API Calls (Port 443/5000) --> DotNet_API
    Admin_Browser -- REST API Calls (Port 443/5000) --> DotNet_API

    DotNet_API -- Appels internes REST / Webhook (Port 8000) --> Laravel_API
    DotNet_API -- EF Core / SQL Connection (Port 3306) --> MySQL_DB
    Laravel_API -- PDO / SQL Connection (Port 3306) --> MySQL_DB
```

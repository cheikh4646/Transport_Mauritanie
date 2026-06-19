-- =========================================================================
-- Schéma de base de données : Gestion du Transport Interurbain en Mauritanie
-- Système cible : MySQL / MariaDB
-- =========================================================================

CREATE DATABASE IF NOT EXISTS `transport_mauritanie_db` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `transport_mauritanie_db`;

-- Désactiver les contraintes temporairement pour une réinitialisation propre
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `tickets`;
DROP TABLE IF EXISTS `payments`;
DROP TABLE IF EXISTS `reservations`;
DROP TABLE IF EXISTS `trips`;
DROP TABLE IF EXISTS `buses`;
DROP TABLE IF EXISTS `companies`;
DROP TABLE IF EXISTS `users`;
SET FOREIGN_KEY_CHECKS = 1;

-- 1. Table des Utilisateurs (Administrateurs, Compagnies, Voyageurs)
CREATE TABLE `users` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `name` VARCHAR(100) NOT NULL,
  `email` VARCHAR(150) NOT NULL UNIQUE,
  `password` VARCHAR(255) NOT NULL, -- Stocke le mot de passe haché (BCrypt)
  `role` ENUM('ADMIN', 'COMPANY', 'TRAVELER') NOT NULL DEFAULT 'TRAVELER',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- 2. Table des Compagnies de Transport
CREATE TABLE `companies` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `name` VARCHAR(100) NOT NULL UNIQUE,
  `phone` VARCHAR(20) NOT NULL,
  `email` VARCHAR(150) NOT NULL UNIQUE,
  `address` VARCHAR(255) NOT NULL,
  `manager_id` INT NULL, -- Optionnel : associer un utilisateur de rôle 'COMPANY' à cette fiche
  `is_active` TINYINT(1) NOT NULL DEFAULT 1,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`manager_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB;

-- 3. Table des Bus
CREATE TABLE `buses` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `company_id` INT NOT NULL,
  `bus_number` VARCHAR(50) NOT NULL, -- Immatriculation
  `capacity` INT NOT NULL, -- Nombre de places (ex: 15, 30, 50)
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 4. Table des Trajets
CREATE TABLE `trips` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `bus_id` INT NOT NULL,
  `departure_city` VARCHAR(100) NOT NULL,
  `arrival_city` VARCHAR(100) NOT NULL,
  `departure_date` DATE NOT NULL,
  `departure_time` TIME NOT NULL,
  `price` DECIMAL(10, 2) NOT NULL,
  `status` ENUM('SCHEDULED', 'ON_GOING', 'ARRIVED', 'CANCELLED') NOT NULL DEFAULT 'SCHEDULED',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (`bus_id`) REFERENCES `buses` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 5. Table des Réservations
CREATE TABLE `reservations` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `user_id` INT NOT NULL,
  `trip_id` INT NOT NULL,
  `seat_number` INT NOT NULL,
  `status` ENUM('PENDING', 'PAID', 'CANCELLED') NOT NULL DEFAULT 'PENDING',
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `unique_seat_trip` (`trip_id`, `seat_number`), -- Empêche la double réservation d'un même siège sur un trajet
  FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  FOREIGN KEY (`trip_id`) REFERENCES `trips` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 6. Table des Paiements
CREATE TABLE `payments` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `reservation_id` INT NOT NULL UNIQUE,
  `amount` DECIMAL(10, 2) NOT NULL,
  `method` ENUM('BANKILY', 'MASRIFY', 'BIMIE', 'CREDIT_CARD') NOT NULL,
  `transaction_reference` VARCHAR(100) NULL, -- Référence retournée par le système de paiement mobile
  `payment_date` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (`reservation_id`) REFERENCES `reservations` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- 7. Table des Billets (Générés après paiement réussi)
CREATE TABLE `tickets` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `reservation_id` INT NOT NULL UNIQUE,
  `qr_code` VARCHAR(255) NOT NULL, -- Chaîne encodée dans le QR Code
  `pdf_path` VARCHAR(255) NULL, -- Chemin d'accès au fichier PDF
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (`reservation_id`) REFERENCES `reservations` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

-- =========================================================================
-- Insertion de données de test (SEED DATA)
-- =========================================================================

-- Mots de passe cryptés de test (tous égaux à "password123", hachés en BCrypt : $2a$10$vI8aWB... ou simulé ici pour compatibilité)
INSERT INTO `users` (`id`, `name`, `email`, `password`, `role`) VALUES
(1, 'Administrateur Général', 'admin@transports.gov.mr', '$2a$12$R9hKBtCudO.y12L2UvJt1uVqO5u6Hk9fA/b.sP0jL6c5cM7d76yL.', 'ADMIN'),
(2, 'Gérant El Moussafir', 'manager@elmoussafir.mr', '$2a$12$R9hKBtCudO.y12L2UvJt1uVqO5u6Hk9fA/b.sP0jL6c5cM7d76yL.', 'COMPANY'),
(3, 'Gérant Sonef', 'manager@sonef.mr', '$2a$12$R9hKBtCudO.y12L2UvJt1uVqO5u6Hk9fA/b.sP0jL6c5cM7d76yL.', 'COMPANY'),
(4, 'Ahmed Voyageur', 'ahmed@gmail.com', '$2a$12$R9hKBtCudO.y12L2UvJt1uVqO5u6Hk9fA/b.sP0jL6c5cM7d76yL.', 'TRAVELER'),
(5, 'Mariem Voyageuse', 'mariem@gmail.com', '$2a$12$R9hKBtCudO.y12L2UvJt1uVqO5u6Hk9fA/b.sP0jL6c5cM7d76yL.', 'TRAVELER');

-- Compagnies de Transport
INSERT INTO `companies` (`id`, `name`, `phone`, `email`, `address`, `manager_id`) VALUES
(1, 'El Moussafir', '+222 45250001', 'contact@elmoussafir.mr', 'Carrefour Madrid, Nouakchott', 2),
(2, 'Sonef Mauritanie', '+222 45250002', 'contact@sonef.mr', 'Avenue Charles de Gaulle, Nouakchott', 3);

-- Bus de test
INSERT INTO `buses` (`id`, `company_id`, `bus_number`, `capacity`) VALUES
(1, 1, '1234AA01', 30), -- Bus El Moussafir de 30 places
(2, 1, '5678AA01', 15), -- Bus El Moussafir de 15 places (Mini-bus)
(3, 2, '9999AB02', 50); -- Bus Sonef de 50 places

-- Trajets de test (Nouakchott vers d'autres villes de Mauritanie)
INSERT INTO `trips` (`id`, `bus_id`, `departure_city`, `arrival_city`, `departure_date`, `departure_time`, `price`, `status`) VALUES
(1, 1, 'Nouakchott', 'Nouadhibou', CURDATE() + INTERVAL 1 DAY, '07:00:00', 800.00, 'SCHEDULED'),
(2, 2, 'Nouakchott', 'Rosso', CURDATE() + INTERVAL 1 DAY, '09:00:00', 300.00, 'SCHEDULED'),
(3, 3, 'Nouakchott', 'Atar', CURDATE() + INTERVAL 2 DAY, '06:30:00', 600.00, 'SCHEDULED'),
(4, 1, 'Nouadhibou', 'Nouakchott', CURDATE() + INTERVAL 2 DAY, '14:00:00', 800.00, 'SCHEDULED');

-- Réservations de test
INSERT INTO `reservations` (`id`, `user_id`, `trip_id`, `seat_number`, `status`) VALUES
(1, 4, 1, 12, 'PAID'),
(2, 5, 1, 13, 'PAID'),
(3, 4, 2, 5, 'PENDING');

-- Paiements de test
INSERT INTO `payments` (`id`, `reservation_id`, `amount`, `method`, `transaction_reference`) VALUES
(1, 1, 800.00, 'BANKILY', 'BKY-TRX-87654'),
(2, 2, 800.00, 'MASRIFY', 'MSF-TRX-12345');

-- Billets de test
INSERT INTO `tickets` (`id`, `reservation_id`, `qr_code`, `pdf_path`) VALUES
(1, 1, 'TICKET-1-TRIP1-SEAT12-87654', '/tickets/ticket_1.pdf'),
(2, 2, 'TICKET-2-TRIP1-SEAT13-12345', '/tickets/ticket_2.pdf');

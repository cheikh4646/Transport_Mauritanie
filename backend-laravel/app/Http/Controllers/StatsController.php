<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;

class StatsController extends Controller
{
    /**
     * Retourne les statistiques globales pour le tableau de bord Admin.
     */
    public function getDashboardStats()
    {
        // En conditions réelles, ces chiffres seraient extraits de MySQL via des requêtes d'agrégation.
        // Ici, on renvoie un jeu de données mockées de haute qualité pour la démo du PFE.
        $stats = [
            'overview' => [
                'total_revenue_mr' => 124500.00, // En Ouguiya (MRU)
                'total_reservations' => 380,
                'active_companies' => 8,
                'total_buses' => 45,
            ],
            'revenue_by_month' => [
                ['month' => 'Janvier', 'revenue' => 18000],
                ['month' => 'Février', 'revenue' => 22000],
                ['month' => 'Mars', 'revenue' => 25000],
                ['month' => 'Avril', 'revenue' => 29000],
                ['month' => 'Mai', 'revenue' => 30500],
            ],
            'popular_routes' => [
                ['route' => 'Nouakchott - Nouadhibou', 'percentage' => 45],
                ['route' => 'Nouakchott - Rosso', 'percentage' => 25],
                ['route' => 'Nouakchott - Atar', 'percentage' => 15],
                ['route' => 'Autres', 'percentage' => 15],
            ],
            'reservations_by_status' => [
                ['status' => 'Confirmées (Payées)', 'count' => 310],
                ['status' => 'En attente', 'count' => 50],
                ['status' => 'Annulées', 'count' => 20],
            ]
        ];

        return response()->json([
            'status' => 'success',
            'data' => $stats
        ], 200);
    }

    /**
     * Retourne les statistiques spécifiques pour une compagnie.
     */
    public function getCompanyStats($companyId)
    {
        // Statistiques d'une compagnie spécifique (simulées)
        $stats = [
            'company_id' => $companyId,
            'overview' => [
                'total_sales_mr' => 48500.00,
                'tickets_sold' => 120,
                'bus_count' => 6,
                'average_occupancy_rate' => 78.5 // Pourcentage de places vendues
            ],
            'recent_activity' => [
                ['date' => now()->subDays(2)->format('Y-m-d'), 'tickets' => 15, 'revenue' => 4500],
                ['date' => now()->subDays(1)->format('Y-m-d'), 'tickets' => 18, 'revenue' => 5400],
                ['date' => now()->format('Y-m-d'), 'tickets' => 10, 'revenue' => 3000],
            ],
            'fleet_occupancy' => [
                ['bus_number' => '1234AA01', 'trips_count' => 12, 'occupancy' => 85.2],
                ['bus_number' => '5678AA01', 'trips_count' => 8, 'occupancy' => 70.0],
            ]
        ];

        return response()->json([
            'status' => 'success',
            'data' => $stats
        ], 200);
    }
}

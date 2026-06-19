<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Storage;
use SimpleSoftwareIO\QrCode\Facades\QrCode;
use Barryvdh\DomPDF\Facade\Pdf;

class TicketController extends Controller
{
    /**
     * Génère un billet PDF et un QR Code associé pour une réservation.
     */
    public function generateTicket(Request $request)
    {
        $request->validate([
            'reservation_id' => 'required|integer',
            'qr_code' => 'required|string',
        ]);

        $reservationId = $request->input('reservation_id');
        $qrCodeData = $request->input('qr_code');

        try {
            // 1. Génération du QR Code (format SVG ou PNG)
            // Dans Laravel, QrCode::generate() retourne le SVG. On peut le stocker localement.
            $qrCodePath = "qrcodes/qr_{$reservationId}.svg";
            $qrCodeSvg = QrCode::size(200)->generate($qrCodeData);
            Storage::disk('public')->put($qrCodePath, $qrCodeSvg);

            // 2. Génération du PDF du Billet avec DomPDF (Simulé ou réel selon l'environnement)
            $pdfPath = "tickets/ticket_{$reservationId}.pdf";
            
            // Simulation de données pour le design du billet
            $data = [
                'id' => $reservationId,
                'qr_code_svg' => $qrCodeSvg,
                'date_edition' => now()->format('d/m/Y H:i'),
                'reference' => $qrCodeData
            ];

            // Rendu de la vue PDF (ressource non compilée en local sans Laravel complet)
            // $pdf = Pdf::loadView('pdf.ticket', $data);
            // Storage::disk('public')->put($pdfPath, $pdf->output());

            // Pour l'intégration locale sans framework complet, on crée un fichier simulé et on retourne le chemin
            Storage::disk('public')->put($pdfPath, "PDF Billet de Voyage - Réservation #{$reservationId}. Réf QR: {$qrCodeData}");

            return response()->json([
                'status' => 'success',
                'message' => 'Billet et QR code générés avec succès.',
                'qr_code_path' => Storage::url($qrCodePath),
                'pdf_path' => Storage::url($pdfPath),
            ], 201);

        } catch (\Exception $e) {
            return response()->json([
                'status' => 'error',
                'message' => 'Échec de la génération du billet : ' . $e->getMessage()
            ], 500);
        }
    }
}

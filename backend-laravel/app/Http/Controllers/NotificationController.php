<?php

namespace App\Http\Controllers;

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class NotificationController extends Controller
{
    /**
     * Envoie une notification (Email / SMS) pour confirmer la réservation.
     */
    public function sendNotification(Request $request)
    {
        $request->validate([
            'email' => 'required|email',
            'phone' => 'required|string',
            'message' => 'required|string',
            'type' => 'required|in:sms,email,both'
        ]);

        $email = $request->input('email');
        $phone = $request->input('phone');
        $message = $request->input('message');
        $type = $request->input('type');

        try {
            if ($type === 'email' || $type === 'both') {
                // Simulation d'envoi d'email
                Log::info("Email envoyé à {$email} : {$message}");
            }

            if ($type === 'sms' || $type === 'both') {
                // Simulation d'envoi de SMS (ex: Twilio ou opérateur local mauritanien)
                Log::info("SMS envoyé au numéro {$phone} : {$message}");
            }

            return response()->json([
                'status' => 'success',
                'message' => 'Notification envoyée avec succès.'
            ], 200);

        } catch (\Exception $e) {
            return response()->json([
                'status' => 'error',
                'message' => 'Erreur lors du traitement de la notification : ' . $e->getMessage()
            ], 500);
        }
    }
}

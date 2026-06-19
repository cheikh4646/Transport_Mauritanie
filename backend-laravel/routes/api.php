<?php

use Illuminate\Support\Facades\Route;
use App\Http\Controllers\TicketController;
use App\Http\Controllers\NotificationController;
use App\Http\Controllers\StatsController;

/*
|--------------------------------------------------------------------------
| API Routes
|--------------------------------------------------------------------------
|
| Here is where you can register API routes for your Laravel service.
| These routes are loaded by the RouteServiceProvider and all of them
| will be assigned to the "api" middleware group. Make something great!
|
*/

// Ticket Generation
Route::post('/tickets/generate', [TicketController::class, 'generateTicket']);

// Notifications (SMS/Email Dispatcher)
Route::post('/notifications/send', [NotificationController::class, 'sendNotification']);

// Consolidated Analytics and Reports
Route::get('/stats/dashboard', [StatsController::class, 'getDashboardStats']);
Route::get('/stats/company/{companyId}', [StatsController::class, 'getCompanyStats']);

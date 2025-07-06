import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService, User } from '../services/auth';
import { WeatherService, WeatherForecast } from '../services/weather';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  template: `
    <div class="dashboard">
      <header class="header">
        <div class="header-content">
          <h1>AI Portfolio Analysis Dashboard</h1>
          <div class="user-info" *ngIf="user">
            <img [src]="user.picture" [alt]="user.name" class="user-avatar" *ngIf="user.picture">
            <span class="user-name">{{ user.name }}</span>
            <button class="logout-btn" (click)="logout()">Logout</button>
          </div>
        </div>
      </header>
      
      <main class="main-content">
        <div class="welcome-section">
          <h2>Welcome back, {{ user?.name }}!</h2>
          <p>Here's your personalized dashboard</p>
        </div>
        
        <div class="weather-section">
          <h3>Weather Forecast</h3>
          <div class="weather-grid" *ngIf="weatherForecast.length > 0; else loading">
            <div class="weather-card" *ngFor="let weather of weatherForecast">
              <div class="date">{{ formatDate(weather.date) }}</div>
              <div class="temperature">{{ weather.temperatureC }}°C</div>
              <div class="summary">{{ weather.summary }}</div>
            </div>
          </div>
          <ng-template #loading>
            <p class="loading">Loading weather data...</p>
          </ng-template>
        </div>
      </main>
    </div>
  `,
  styles: [`
    .dashboard {
      min-height: 100vh;
      background: #f5f5f5;
    }
    
    .header {
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      padding: 1rem 0;
    }
    
    .header-content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 2rem;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    
    h1 {
      color: #333;
      margin: 0;
    }
    
    .user-info {
      display: flex;
      align-items: center;
      gap: 1rem;
    }
    
    .user-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
    }
    
    .user-name {
      font-weight: 500;
      color: #333;
    }
    
    .logout-btn {
      padding: 8px 16px;
      background: #dc3545;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
    }
    
    .logout-btn:hover {
      background: #c82333;
    }
    
    .main-content {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem;
    }
    
    .welcome-section {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      margin-bottom: 2rem;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    
    .weather-section {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }
    
    .weather-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1rem;
      margin-top: 1rem;
    }
    
    .weather-card {
      background: #f8f9fa;
      padding: 1.5rem;
      border-radius: 6px;
      text-align: center;
      border: 1px solid #e9ecef;
    }
    
    .date {
      font-weight: 500;
      color: #495057;
      margin-bottom: 0.5rem;
    }
    
    .temperature {
      font-size: 1.5rem;
      font-weight: bold;
      color: #007bff;
      margin-bottom: 0.5rem;
    }
    
    .summary {
      color: #6c757d;
      font-size: 0.9rem;
    }
    
    .loading {
      text-align: center;
      color: #6c757d;
      font-style: italic;
    }
  `]
})
export class DashboardComponent implements OnInit {
  user: User | null = null;
  weatherForecast: WeatherForecast[] = [];

  constructor(
    private authService: AuthService,
    private weatherService: WeatherService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Check for user data in query params (from OAuth callback)
    this.route.queryParams.subscribe(params => {
      if (params['user']) {
        try {
          const userData = JSON.parse(decodeURIComponent(params['user']));
          this.authService.setUser(userData);
          this.user = userData;
          // Remove query params from URL
          this.router.navigate([], { queryParams: {} });
          // Load weather data after successful login
          this.loadWeatherForecast();
        } catch (e) {
          console.error('Error parsing user data from URL');
          this.checkAuthAndRedirect();
        }
      } else {
        this.checkAuthAndRedirect();
      }
    });

    // Subscribe to auth service for user updates
    this.authService.user$.subscribe(user => {
      this.user = user;
    });
  }

  private checkAuthAndRedirect(): void {
    // Initialize auth check
    this.authService.initializeAuth();
    
    // Wait a moment for auth to initialize, then check
    setTimeout(() => {
      if (!this.authService.isAuthenticated) {
        this.router.navigate(['/login']);
      } else {
        this.user = this.authService.currentUser;
        this.loadWeatherForecast();
      }
    }, 100);
  }

  loadWeatherForecast(): void {
    this.weatherService.getWeatherForecast().subscribe({
      next: (data) => {
        this.weatherForecast = data;
      },
      error: (error) => {
        console.error('Error loading weather forecast:', error);
      }
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Logout error:', error);
        // Force logout even if API call fails
        this.router.navigate(['/login']);
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { 
      weekday: 'short', 
      month: 'short', 
      day: 'numeric' 
    });
  }
}
import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { WeatherService, WeatherForecast } from './services/weather';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected title = 'AI Portfolio Analysis';
  weatherForecast: WeatherForecast[] = [];

  constructor(private weatherService: WeatherService) {}

  ngOnInit() {
    this.loadWeatherForecast();
  }

  loadWeatherForecast() {
    this.weatherService.getWeatherForecast().subscribe({
      next: (data) => {
        this.weatherForecast = data;
      },
      error: (error) => {
        console.error('Error loading weather forecast:', error);
      }
    });
  }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface User {
  name: string;
  email: string;
  picture?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor(private http: HttpClient) {
    this.checkAuthStatus();
  }

  login(): void {
    window.location.href = `${environment.apiUrl}/api/auth/login`;
  }

  logout(): Observable<any> {
    return this.http.post(`${environment.apiUrl}/api/auth/logout`, {});
  }

  checkAuthStatus(): void {
    this.http.get<User>(`${environment.apiUrl}/api/auth/user`)
      .subscribe({
        next: (user) => this.userSubject.next(user),
        error: () => this.userSubject.next(null)
      });
  }

  get currentUser(): User | null {
    return this.userSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }
}
import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  errorMessage: string = '';
  isLoading: boolean = false;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(email: string, password: string): void {
    this.errorMessage = '';
    this.isLoading = true;

    this.authService.login(email, password).subscribe({
      next: (response) => {
        // Erfolgreich, kein Fehlerobjekt
        this.isLoading = false;
        this.router.navigate(['']);
      },
      error: (error) => {
        this.isLoading = false;
        // Supabase Fehler kann eine AuthApiError Instanz sein
        this.errorMessage = error?.message || 'Login failed.';
      }
    });
  }
}

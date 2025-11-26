import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  errorMessage: string = '';
  isLoading: boolean = false;
  formData = {
    email: '',
    username: '',
    password: ''
  };

  constructor(private authService: AuthService, private router: Router) { }

  onSubmit(email: string, username: string, password: string): void {
    this.errorMessage = '';
    this.isLoading = true;

    this.authService.register(email, username, password).subscribe({
      next: (response) => {
        this.isLoading = false;
        // Nur navigieren, wenn ein User zurückkam
        if (response.data.user) {
          this.router.navigate(['/login']);
        } else {
          this.errorMessage = 'Registration failed (no user object).';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error?.message || 'Registration failed.';
      }
    });
  }
}
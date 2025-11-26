import { Injectable, signal, inject } from "@angular/core";
import { AuthResponse } from "@supabase/supabase-js";
import { from, Observable, of, throwError } from "rxjs";
import { mergeMap } from 'rxjs/operators';
import { SupabaseService } from './supabase.service';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    // Auth service implementation goes here
    private supabaseService = inject(SupabaseService);
    supabase = this.supabaseService.client;
    currentUser = signal<{ email: string; username: string } | null>(null);

    register(email: string, username: string, password: string): Observable<AuthResponse> {
        const cleanedEmail = email.trim();
        const cleanedUsername = username.trim();
        const promise = this.supabase.auth.signUp({
            email: cleanedEmail,
            password,
            options: { data: { username: cleanedUsername } },
        });
        return from(promise).pipe(
            mergeMap(resp => resp.error ? throwError(() => resp.error) : of(resp))
        );
    }

    login(email: string, password: string): Observable<AuthResponse> {
        const cleanedEmail = email.trim();
        const promise = this.supabase.auth.signInWithPassword({
            email: cleanedEmail,
            password,
        });
        return from(promise).pipe(
            mergeMap(resp => resp.error ? throwError(() => resp.error) : of(resp))
        );
    }

    logout(): void {
        this.supabase.auth.signOut();
    }
}
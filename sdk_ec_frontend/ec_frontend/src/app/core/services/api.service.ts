import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, from, switchMap, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SupabaseService } from './supabase.service';

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private apiUrl = environment.apiUrl;
    private supabaseService = inject(SupabaseService);

    constructor(private http: HttpClient) { }

    /**
     * Gets the current session and returns headers with authorization token
     */
    private async getHeaders(): Promise<HttpHeaders> {
        const { data: { session } } = await this.supabaseService.client.auth.getSession();
        let headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        
        if (session?.access_token) {
            headers = headers.set('Authorization', `Bearer ${session.access_token}`);
        }
        
        return headers;
    }

    /**
     * Performs a GET request
     * @param path The API endpoint path
     * @param params Optional query parameters
     * @returns Observable of the response
     */
    get<T>(path: string, params: HttpParams = new HttpParams()): Observable<T> {
        return from(this.getHeaders()).pipe(
            switchMap(headers => this.http.get<T>(`${this.apiUrl}${path}`, { headers, params })),
            map(response => this.convertTimestampsToRigaTime(response))
        );
    }

    /**
     * Performs a POST request
     * @param path The API endpoint path
     * @param body The request body
     * @returns Observable of the response
     */
    post<T>(path: string, body: any): Observable<T> {
        return from(this.getHeaders()).pipe(
            switchMap(headers => this.http.post<T>(`${this.apiUrl}${path}`, body, { headers })),
            map(response => this.convertTimestampsToRigaTime(response))
        );
    }

    /**
     * Performs a PUT request
     * @param path The API endpoint path
     * @param body The request body
     * @returns Observable of the response
     */
    put<T>(path: string, body: any): Observable<T> {
        return from(this.getHeaders()).pipe(
            switchMap(headers => this.http.put<T>(`${this.apiUrl}${path}`, body, { headers })),
            map(response => this.convertTimestampsToRigaTime(response))
        );
    }

    /**
     * Performs a PATCH request
     * @param path The API endpoint path
     * @param body The request body
     * @returns Observable of the response
     */
    patch<T>(path: string, body: any): Observable<T> {
        return from(this.getHeaders()).pipe(
            switchMap(headers => this.http.patch<T>(`${this.apiUrl}${path}`, body, { headers })),
            map(response => this.convertTimestampsToRigaTime(response))
        );
    }

    /**
     * Performs a DELETE request
     * @param path The API endpoint path
     * @returns Observable of the response
     */
    delete<T>(path: string): Observable<T> {
        return from(this.getHeaders()).pipe(
            switchMap(headers => this.http.delete<T>(`${this.apiUrl}${path}`, { headers })),
            map(response => this.convertTimestampsToRigaTime(response))
        );
    }

    /**
     * Recursively converts UTC timestamps to Riga timezone (UTC+2)
     * Detects common timestamp field names and converts them
     */
    private convertTimestampsToRigaTime<T>(data: T): T {
        if (!data) return data;

        // Handle arrays
        if (Array.isArray(data)) {
            return data.map(item => this.convertTimestampsToRigaTime(item)) as any;
        }

        // Handle objects
        if (typeof data === 'object') {
            const converted: any = {};
            for (const [key, value] of Object.entries(data)) {
                // Check if field name suggests it's a timestamp
                const isTimestampField = /date|time|at$/i.test(key);
                
                if (isTimestampField && typeof value === 'string' && this.isISODateString(value)) {
                    // Convert UTC to Riga time (+2 hours)
                    const utcDate = new Date(value);
                    const rigaDate = new Date(utcDate.getTime() + (2 * 60 * 60 * 1000));
                    converted[key] = rigaDate.toISOString();
                } else if (typeof value === 'object') {
                    // Recursively convert nested objects
                    converted[key] = this.convertTimestampsToRigaTime(value);
                } else {
                    converted[key] = value;
                }
            }
            return converted as T;
        }

        return data;
    }

    /**
     * Check if a string is a valid ISO date string
     */
    private isISODateString(value: string): boolean {
        const isoDateRegex = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$/;
        return isoDateRegex.test(value) && !isNaN(Date.parse(value));
    }
}
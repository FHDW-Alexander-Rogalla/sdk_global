import { Injectable } from '@angular/core';
import { createClient, SupabaseClient } from '@supabase/supabase-js';
import { environment } from '../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class SupabaseService {
    private static instance: SupabaseClient | null = null;

    get client(): SupabaseClient {
        if (!SupabaseService.instance) {
            SupabaseService.instance = createClient(
                environment.supabaseUrl,
                environment.supabaseKey,
                {
                    auth: {
                        autoRefreshToken: true,
                        persistSession: true,
                        detectSessionInUrl: true,
                        flowType: 'pkce'
                    }
                }
            );
        }
        return SupabaseService.instance;
    }
}

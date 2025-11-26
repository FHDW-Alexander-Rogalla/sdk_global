namespace Sdk_EC_Backend.Configuration;

public class SupabaseSettings
{
    public const string SectionName = "Supabase";

    // Preferred for PostgREST / supabase client usage
    public string Url { get; set; } = string.Empty;
    
    // Service Role Key for backend operations (bypasses RLS)
    // Get from: Supabase Dashboard > Settings > API > service_role key
    public string Key { get; set; } = string.Empty;

    // JWT secret for validating Supabase access tokens (from Supabase Dashboard > Settings > API > JWT Secret)
    public string? JwtSecret { get; set; }

    // Optional DB connection fields (left for backward compatibility if needed)
    public string Host { get; set; } = string.Empty;
    public string Database { get; set; } = "postgres";
    public string Schema { get; set; } = "public";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = string.Empty;
}
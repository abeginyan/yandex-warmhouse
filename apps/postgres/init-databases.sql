-- Pre-create application databases so EF Core migrations connect directly
-- without Npgsql needing to fall back to the admin 'postgres' database first.
CREATE DATABASE smarthome;
CREATE DATABASE users;
CREATE DATABASE temperature;

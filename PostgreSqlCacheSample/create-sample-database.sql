-- This script is for be used with this sample
-- It requires a superuser login since creates database and users
-- by default 'postgres' user account

CREATE ROLE cache_test WITH LOGIN CREATEDB PASSWORD 't3st';
CREATE DATABASE cache_test OWNER cache_test;
\c "user=cache_test dbname=cache_test password=t3st"
CREATE SCHEMA IF NOT EXISTS name1 AUTHORIZATION cache_test;

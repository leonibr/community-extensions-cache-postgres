@echo off
set arg1=%1
if "%arg1%"=="-erase" (
	psql -U postgres -f erase-sample-database.sql
) 
if "%arg1%"=="-create" (
echo 1^. Create database 'cache_test' into localhost
echo using default postgres user
echo 2^. Create a role also called 'cache_test'
echo 3^. Connects to database and create a schema 'name1'
	psql -U postgres -f create-sample-database.sql
)
echo Done.




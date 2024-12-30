#!/bin/bash

show_usage() {
    echo "Usage: ./prepare-database.sh [-create|-erase]"
    echo "  -create    Create the sample database and role"
    echo "  -erase     Erase the sample database"
}

if ! command -v psql &> /dev/null; then
    echo "Error: psql command not found. Please ensure PostgreSQL is installed and in your PATH."
    exit 1
fi

# if no arguments are provided, show usage and exit
if [ $# -eq 0 ]; then
    show_usage
    exit 1
fi

case "$1" in
    "-erase")
        echo "Erasing database..."
        psql -U postgres -f erase-sample-database.sql
        ;;
    "-create")
        echo "1. Create database 'cache_test' into localhost"
        echo "2. Create a role also called 'cache_test'"
        echo "3. Connects to database and create a schema 'name1'"
        psql -U postgres -f create-sample-database.sql
        ;;
    *)
        show_usage
        exit 1
        ;;
esac

echo "Done." 
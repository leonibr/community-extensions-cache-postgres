#!/bin/bash

echo "PostgreSQL Distributed Cache Benchmarks"
echo "========================================"
echo

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "ERROR: Docker is not running or not accessible."
    echo "Please start Docker service and try again."
    exit 1
fi

echo "Docker is running. Starting benchmarks..."
echo

# Set configuration to Release for accurate benchmarks
CONFIGURATION=Release

# Check if specific benchmark was requested
if [ -z "$1" ]; then
    echo "Running all benchmarks..."
    dotnet run --configuration $CONFIGURATION
else
    echo "Running $1 benchmark..."
    dotnet run --configuration $CONFIGURATION -- $1
fi

echo
echo "Benchmarks completed!"
echo "Results can be found in BenchmarkDotNet.Artifacts/results/"
echo 
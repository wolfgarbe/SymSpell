#!/bin/bash
set -e

CERT_PATH=SymSpell.SWS/sws-test-cert.pem
echo "Exporting dev cert for testing to $CERT_PATH..."
dotnet dev-certs https --format PEM --export-path $CERT_PATH

# Ensure cert is removed and web service is killed on exit
trap 'echo "Stopping web service..."; kill $SWS_PID; echo "Cleaning up cert file..."; rm -f $CERT_PATH' EXIT

echo "Starting SymSpell.SWS web service..."
dotnet run --project SymSpell.SWS/SymSpell.SWS.csproj &
SWS_PID=$!

echo "Waiting for service to start..."
sleep 10

echo "Running test script..."
python3 SymSpell.SWS/test_sws.py $CERT_PATH

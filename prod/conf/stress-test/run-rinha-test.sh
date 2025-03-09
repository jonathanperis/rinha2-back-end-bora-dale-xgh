#!/bin/sh

echo "Tests will start in 15 seconds..."
sleep 15

# Run the k6 test
k6 run rinha-test.js

# Fix the permissions on the generated report before copying it.
if [ -f stress-test-report.html ]; then
    chmod 644 stress-test-report.html
    cp stress-test-report.html /reports/
    echo "Report copied to /reports/stress-test-report.html"
else
    echo "Report not found!"
fi
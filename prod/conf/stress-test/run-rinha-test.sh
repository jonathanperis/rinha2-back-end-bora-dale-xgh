#!/bin/sh

# Ensure the /reports directory exists (creates it if not, with appropriate permissions)
mkdir -p /reports

echo "Tests will start in 15 seconds..."
sleep 15

echo "Starting test (estimation: 5 minutes)..."

# Run the k6 test
k6 run rinha-test.js --quiet

echo "Finished tests..."

# Fix the permissions on the generated report before copying it.
if [ -f stress-test-report.html ]; then
    chmod 644 stress-test-report.html
    cp stress-test-report.html /reports/
    echo "Report copied to /reports/stress-test-report.html"
else
    echo "Report not found!"
fi

# List contents of /reports to confirm file presence
echo "Contents of /reports:"
ls -l /reports
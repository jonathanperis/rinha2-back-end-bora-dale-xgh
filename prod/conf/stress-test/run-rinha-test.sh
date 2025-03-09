#!/bin/sh

echo "Tests will start in 15 seconds..."
sleep 15

sudo k6 run rinha-test.js

# After the test run, copy the exported report to the mounted /reports directory
if [ -f stress-test-report.html ]; then
    cp stress-test-report.html /reports/
    echo "Report copied to /reports/stress-test-report.html"
else
    echo "Report not found!"
fi
#!/bin/sh

echo "Tests will start in 15 seconds..."
sleep 15

k6 run rinha-test.js -o xk6-influxdb
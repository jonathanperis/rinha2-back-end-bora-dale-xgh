#!/bin/sh

echo "Tests will start in 15 seconds..."
sleep 15

chmod -c -R +rX "_site/" | while read line; do
    echo "::warning title=Invalid file permissions automatically fixed::$line"
done

k6 run rinha-test.js
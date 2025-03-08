#!/bin/sh

sleep 10

k6 run rinha-test.js -o xk6-influxdb

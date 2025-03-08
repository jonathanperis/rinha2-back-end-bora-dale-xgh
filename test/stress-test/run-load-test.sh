#!/bin/sh

sleep 10

k6 run load-test.js -o xk6-influxdb
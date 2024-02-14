#!/bin/bash
set -e

psql -U postgres -w rinha < ./rinha.dump.sql

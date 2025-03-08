#!/bin/sh

sleep 10

k6 run validacoes/concorentes-d.js
k6 run validacoes/concorentes-c.js
k6 run validacoes/criterios-clientes.js
k6 run validacoes/criterio-cliente-nao-encontrado.js
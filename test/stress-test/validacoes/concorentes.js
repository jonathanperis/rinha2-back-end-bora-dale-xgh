// @ts-check

import { check } from 'k6'
import http from 'k6/http'

export const numRequests = 25

const baseUrl = __ENV.BASE_URL || "http://localhost:9999";

/**
 * @param {'c'|'d'} tipo 
 */
export function criarTransacao(tipo) {
    const resp = http.post(
        `${baseUrl}/clientes/1/transacoes`,
        JSON.stringify({ valor: 1, tipo: tipo, descricao: 'validacao' }),
        {
            headers: {
                'content-type': 'application/json',
            }
        }
    )
    check(resp, {
        'check status 200': (resp) => resp.status == 200,
    })
}

/**
 * @param {Number} saldoExperado
 */
export function validarSaldo(saldoExperado) {
    const resp = http.get(`${baseUrl}/clientes/1/extrato`)
    check(resp, {
        'check status 200': (resp) => resp.status == 200,
        'check saldo': (resp) => resp.json('saldo.total') == saldoExperado,
    })
}
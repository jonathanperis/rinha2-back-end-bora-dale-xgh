import { check } from 'k6'
import http from 'k6/http'
import { randomString } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js'

const baseUrl = __ENV.BASE_URL || "http://localhost:9999";

const randomClientId = () => 1 + Math.floor(Math.random() * 5)
const randomValorTransacao = () => 1 + Math.floor(Math.random() * 10000)
const randomDescricao = () => randomString(10)

export function debitos() {
    const descricao = randomDescricao()
    const valor = randomValorTransacao()
    const clienteId = randomClientId()

    const resp = http.post(
        `${baseUrl}/clientes/${clienteId}/transacoes`,
        JSON.stringify({ valor, tipo: 'd', descricao }),
        {
            responseType: 'text',
            headers: {
                'content-type': 'application/json',
            }
        }
    )
    check(resp, {
        'check status in 200|422': (resp) => [200, 422].includes(resp.status),
    })
    if (resp.status == 200) {
        validarLimiteDeTransacao(resp.body)
    }
}

export const creditos = () => {
    const descricao = randomDescricao()
    const valor = randomValorTransacao()
    const clienteId = randomClientId()

    const resp = http.post(
        `${baseUrl}/clientes/${clienteId}/transacoes`,
        JSON.stringify({ valor, tipo: 'c', descricao }),
        {
            responseType: 'text',
            headers: {
                'content-type': 'application/json',
            }
        }
    )
    check(resp, {
        'check status in 200|422': (resp) => [200, 422].includes(resp.status),
    })
    validarLimiteDeTransacao(resp.body)
}

export const extratos = () => {
    const resp = http.get(
        `${baseUrl}/clientes/${randomClientId()}/extrato`,
        { responseType: 'text' }
    )
    check(resp, {
        'check status 200': (resp) => resp.status == 200,
    })
    validarLimiteDoExtrato(resp.body)
}

/**
 * @param {String} extratoBody 
 */
export function validarLimiteDoExtrato(extratoBody) {
    const json = JSON.parse(extratoBody)
    const saldo = json['saldo']['total']
    const limite = json['saldo']['limite']
    validarLimite(saldo, limite)
}

/**
 * @param {String} transacaoBody 
 */
export function validarLimiteDeTransacao(transacaoBody) {
    const json = JSON.parse(transacaoBody)
    const saldo = json['saldo']
    const limite = json['limite']
    return validarLimite(saldo, limite)
}

/**
 * @param {Number} saldo 
 * @param {Number} limite 
 */
function validarLimite(saldo, limite) {
    return check({ saldo, limite }, {
        'check limite': (v) => v.saldo >= v.limite * -1,
    })
}

/**
 * @typedef {import('k6/options').Options} Options
 * @type {Options}
 */
export const options = {
    userAgent: 'Agente do Caos - 2024/Q1',
    scenarios: {
        debitos: {
            exec: 'debitos',
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '2m', target: 220 },
                { duration: '2m', target: 220 },
            ]
        },
        creditos: {
            exec: 'creditos',
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '2m', target: 110 },
                { duration: '2m', target: 110 },
            ]
        },
        extratos: {
            exec: 'extratos',
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '2m', target: 10 },
                { duration: '2m', target: 10 },
            ]
        },
    }
}
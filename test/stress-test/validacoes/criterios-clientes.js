// @ts-check

import { check, fail } from 'k6'
import http from 'k6/http'
import { validarLimiteDeTransacao } from '../load-test.js'

const baseUrl = __ENV.BASE_URL || "http://localhost:9999";

const clientes = [
    { id: 1, limite: 1000 * 100 },
    { id: 2, limite: 800 * 100 },
    { id: 3, limite: 10000 * 100 },
    { id: 4, limite: 100000 * 100 },
    { id: 5, limite: 5000 * 100 },
]

/**
 * @param {*} clienteId 
 * @param {*} valor
 * @param {*} tipo
 * @param {*} descricao
 */
function criarTransacao(clienteId, valor, tipo, descricao) {
    return http.post(
        `${baseUrl}/clientes/${clienteId}/transacoes`,
        JSON.stringify({ valor, tipo, descricao }),
        {
            responseType: 'text',
            headers: {
                'content-type': 'application/json',
            }
        }
    )
}

/**
 * 
 * @param {Number} id 
 * @param {Number} limite 
 */
function testarCriteriosCliente(id, limite) {
    // Extrato
    const respExtrato1 = http.get(`${baseUrl}/clientes/${id}/extrato`, { responseType: 'text' })
    check(respExtrato1, {
        'check extrato 200': (resp) => resp.status == 200,
    })
    const jsonExtrato1 = JSON.parse(respExtrato1.body)
    check(jsonExtrato1, {
        'saldo OK': (json) => json.saldo.total == 0,
        'limite OK': (json) => json.saldo.limite == limite,
    })

    // Credito
    const creditoResp = criarTransacao(id, 1, 'c', 'toma')
    check(creditoResp, {
        'status OK': (resp) => resp.status == 200,
        'limite OK': (resp) => validarLimiteDeTransacao(resp.body),
    })

    // Debito
    const debitoResp = criarTransacao(id, 1, 'd', 'devolve')
    check(creditoResp, {
        'status OK': (resp) => resp.status == 200,
        'limite OK': (resp) => validarLimiteDeTransacao(resp.body),
    })

    // Extrato
    const respExtrato2 = http.get(`${baseUrl}/clientes/${id}/extrato`, { responseType: 'text' })
    check(respExtrato2, {
        'check extrato 200': (resp) => resp.status == 200,
    })
    const jsonExtrato2 = JSON.parse(respExtrato2.body)
    check(jsonExtrato2, {
        '[0] descricao': (json) => json.ultimas_transacoes[0].descricao == 'devolve',
        '[0] tipo     ': (json) => json.ultimas_transacoes[0].tipo == 'd',
        '[0] valor    ': (json) => json.ultimas_transacoes[0].valor == 1,
        '[1] descricao': (json) => json.ultimas_transacoes[1].descricao == 'toma',
        '[1] tipo     ': (json) => json.ultimas_transacoes[1].tipo == 'c',
        '[1] valor    ': (json) => json.ultimas_transacoes[1].valor == 1,
    })

    // Consistencia do extrato
    const danadaResp = criarTransacao(id, 1, 'c', 'danada')
    check(creditoResp, {
        'status OK': (resp) => resp.status == 200,
    })
    const danadaJson = JSON.parse(danadaResp.body)
    // 5 consultas simultâneas ao extrato para verificar consistência
    const batchReq = Array(5).fill({
        method: 'GET',
        url: `${baseUrl}/clientes/${id}/extrato`,
    })
    const batchResp = http.batch(batchReq)
    batchResp.forEach(resp => {
        const body = resp.body
        if(typeof body != 'string') return
        const json = JSON.parse(body)
        check(json, {
            '[0] descricao': (json) => json.ultimas_transacoes[0].descricao == 'danada',
            '[0] tipo     ': (json) => json.ultimas_transacoes[0].tipo == 'c',
            '[0] valor    ': (json) => json.ultimas_transacoes[0].valor == 1,
            'saldo OK': (json) => json.saldo.total == danadaJson.saldo,
            'limite OK': (json) => json.saldo.limite = danadaJson.limite,
        })
    })

    const transacaoRuin1 = criarTransacao(id, 1.2, 'd', 'devolve')
    check(transacaoRuin1, {
        'status 400|422': (req) => [400, 422].includes(req.status)
    })
    const transacaoRuin2 = criarTransacao(id, 1, 'x', 'devolve')
    check(transacaoRuin2, {
        'status 400|422': (req) => [400, 422].includes(req.status)
    })
    const transacaoRuin3 = criarTransacao(id, 1, 'c', '123456789 e mais um pouco')
    check(transacaoRuin3, {
        'status 400|422': (req) => [400, 422].includes(req.status)
    })
    const transacaoRuin4 = criarTransacao(id, 1, 'c', '')
    check(transacaoRuin4, {
        'status 400|422': (req) => [400, 422].includes(req.status)
    })
    const transacaoRuin5 = criarTransacao(id, 1, 'c', null)
    check(transacaoRuin5, {
        'status 400|422': (req) => [400, 422].includes(req.status)
    })
}


export default function () {
    for (let cliente of clientes) {
        testarCriteriosCliente(cliente.id, cliente.limite)
    }
}
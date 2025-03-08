import {criarTransacao, validarSaldo, numRequests} from './concorentes.js'

export default function() {
    criarTransacao('c')
}

export function teardown() {
    validarSaldo(0)
}

export const options = {
    vus: numRequests,
    iterations: numRequests,
}
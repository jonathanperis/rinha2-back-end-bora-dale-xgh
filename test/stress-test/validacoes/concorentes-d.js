import {criarTransacao, validarSaldo, numRequests} from './concorentes.js'

export default function() {
    criarTransacao('d')
}

export function teardown() {
    validarSaldo(numRequests * -1)
}

export const options = {
    vus: numRequests,
    iterations: numRequests,
}
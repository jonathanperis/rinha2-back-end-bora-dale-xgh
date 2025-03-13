import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { Trend } from 'k6/metrics';

// Base URL is read from environment variable. Defaults to localhost if not provided.
const baseUrl = __ENV.BASE_URL || "http://localhost:9999";

// -----------------------------------------------------------------------------
// Custom Trend Metrics
// We are creating five Trend metrics to track the duration of requests per scenario:
// - debitosTrend for the "debitos" scenario.
// - creditosTrend for the "creditos" scenario.
// - extratosTrend for the "extratos" scenario.
// - validacoesTrend for the "validacoes" scenario.
// - clienteNaoEncontradoTrend for the "cliente_nao_encontrado" scenario.
export let debitosTrend = new Trend('debitos_duration', true);
export let creditosTrend = new Trend('creditos_duration', true);
export let extratosTrend = new Trend('extratos_duration', true);
export let validacoesTrend = new Trend('validacoes_duration', true);
export let clienteNaoEncontradoTrend = new Trend('cliente_nao_encontrado_duration', true);

// -----------------------------------------------------------------------------
// Utility functions to generate random test data.
// -----------------------------------------------------------------------------
const randomClienteId = () => Math.floor(Math.random() * 5) + 1;
const randomValorTransacao = () => Math.floor(Math.random() * 10000) + 1;
const randomDescricao = () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  return Array.from({ length: 10 }, () => chars[Math.floor(Math.random() * chars.length)]).join('');
};

// Validate that the client's current balance does not exceed its allowed negative limit.
const validateSaldoLimite = (saldo, limite) => {
  return saldo >= limite * -1;
};

// Shared data for initial client balances is stored in a SharedArray for better performance.
const saldosIniciaisClientes = new SharedArray('clientes', () => [
  { id: 1, limite: 1000 * 100 },
  { id: 2, limite: 800 * 100 },
  { id: 3, limite: 10000 * 100 },
  { id: 4, limite: 100000 * 100 },
  { id: 5, limite: 5000 * 100 },
]);

// -----------------------------------------------------------------------------
// k6 Options and Scenarios
// Each scenario specifies a different test group:
// - validacoes: performs validation tests for client limits and balance consistency.
// - cliente_nao_encontrado: tests the “not found” case.
// - debitos and creditos: simulate ramping up virtual users (VUs) for debit and credit operations.
// - extratos: simulates retrieval of a client's statement.
// -----------------------------------------------------------------------------
export const options = {
  scenarios: {
    validacoes: {
      executor: 'per-vu-iterations',
      vus: saldosIniciaisClientes.length,
      iterations: 1,
      startTime: '0s',
      exec: 'validacoes',
    },
    cliente_nao_encontrado: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      startTime: '0s',
      exec: 'cliente_nao_encontrado',
    },
    debitos: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '2m', target: 220 },
        { duration: '2m', target: 220 },
      ],
      startTime: '10s',
      exec: 'debitos',
    },
    creditos: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '2m', target: 110 },
        { duration: '2m', target: 110 },
      ],
      startTime: '10s',
      exec: 'creditos',
    },
    extratos: {
      executor: 'per-vu-iterations',
      vus: 10,
      iterations: 1,
      startTime: '10s',
      exec: 'extratos',
    },
  },
};

// -----------------------------------------------------------------------------
// Test function for debit transactions
// -----------------------------------------------------------------------------
export function debitos() {
  const payload = JSON.stringify({
    valor: randomValorTransacao(),
    tipo: 'd',  // 'd' indicates debit
    descricao: randomDescricao(),
  });
  
  const url = `${baseUrl}/clientes/${randomClienteId()}/transacoes`;
  const res = http.post(url, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: '/clientes/:id/transacoes' },
  });

  // Record response duration specifically for debitos.
  debitosTrend.add(res.timings.duration);

  // Check if status is either 200 OK or 422 for invalid/debit cases.
  check(res, {
    'status 200 or 422': (r) => [200, 422].includes(r.status),
  });

  // Validate JSON response if status is 200.
  if (res.status === 200) {
    check(res, {
      'Consistência saldo/limite': (r) => {
        try {
          const saldo = r.json('saldo');
          const limite = r.json('limite');
          return validateSaldoLimite(saldo, limite);
        } catch (e) {
          return false;
        }
      }
    });
  }
}

// -----------------------------------------------------------------------------
// Test function for credit transactions
// -----------------------------------------------------------------------------
export function creditos() {
  const payload = JSON.stringify({
    valor: randomValorTransacao(),
    tipo: 'c',  // 'c' indicates credit
    descricao: randomDescricao(),
  });

  const url = `${baseUrl}/clientes/${randomClienteId()}/transacoes`;
  const res = http.post(url, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: '/clientes/:id/transacoes' },
  });

  // Record response duration specifically for creditos.
  creditosTrend.add(res.timings.duration);

  check(res, {
    'status 200': (r) => r.status === 200,
  });

  // Validate if status is 200 then check JSON consistency.
  if (res.status === 200) {
    check(res, {
      'Consistência saldo/limite': (r) => {
        try {
          const saldo = r.json('saldo');
          const limite = r.json('limite');
          return validateSaldoLimite(saldo, limite);
        } catch (e) {
          return false;
        }
      }
    });
  }
}

// -----------------------------------------------------------------------------
// Test function for fetching client statements (extratos)
// -----------------------------------------------------------------------------
export function extratos() {
  const url = `${baseUrl}/clientes/${randomClienteId()}/extrato`;
  const res = http.get(url, {
    tags: { endpoint: '/clientes/:id/extrato' },
  });
  
  // Record response duration specifically for extratos.
  extratosTrend.add(res.timings.duration);

  check(res, {
    'status 200': (r) => r.status === 200,
  });

  // Validate that the statement's balance and limit are consistent.
  if (res.status === 200) {
    check(res, {
      'Consistência extrato': (r) => {
        try {
          return validateSaldoLimite(
            r.json('saldo.total'),
            r.json('saldo.limite')
          );
        } catch (e) {
          return false;
        }
      }
    });
  }
}

// -----------------------------------------------------------------------------
// Test function for validations that includes multiple steps.
// -----------------------------------------------------------------------------
export function validacoes() {
  // Each virtual user accesses a different client from our SharedArray.
  const index = (__VU - 1) % saldosIniciaisClientes.length;
  const cliente = saldosIniciaisClientes[index];
  
  group('Validações cliente', () => {
    let url = `${baseUrl}/clientes/${cliente.id}/extrato`;
    // GET request to check the client's current statement.
    let res = http.get(url, { tags: { endpoint: '/clientes/:id/extrato' } });
    validacoesTrend.add(res.timings.duration);
    check(res, {
      'status 200': (r) => r.status === 200,
      'limite correto': (r) => r.json('saldo.limite') === cliente.limite,
      'saldo inicial 0': (r) => r.json('saldo.total') === 0,
    });

    url = `${baseUrl}/clientes/${cliente.id}/transacoes`;
    // POST a credit transaction.
    res = http.post(
      url,
      JSON.stringify({ valor: 1, tipo: 'c', descricao: 'toma' }),
      {
        headers: { 'Content-Type': 'application/json' },
        tags: { endpoint: '/clientes/:id/transacoes' },
      }
    );
    validacoesTrend.add(res.timings.duration);
    check(res, {
      'status 200': (r) => r.status === 200,
      'Consistência saldo/limite': (r) =>
        validateSaldoLimite(r.json('saldo'), r.json('limite')),
    });

    // POST a debit transaction.
    res = http.post(
      url,
      JSON.stringify({ valor: 1, tipo: 'd', descricao: 'devolve' }),
      {
        headers: { 'Content-Type': 'application/json' },
        tags: { endpoint: '/clientes/:id/transacoes' },
      }
    );
    validacoesTrend.add(res.timings.duration);
    check(res, {
      'status 200': (r) => r.status === 200,
      'Consistência saldo/limite': (r) =>
        validateSaldoLimite(r.json('saldo'), r.json('limite')),
    });

    sleep(1);

    url = `${baseUrl}/clientes/${cliente.id}/extrato`;
    // GET request to verify recent transactions.
    res = http.get(url, { tags: { endpoint: '/clientes/:id/extrato' } });
    validacoesTrend.add(res.timings.duration);
    check(res, {
      'transações recentes': (r) => {
        const transacoes = r.json('ultimas_transacoes');
        return transacoes &&
          transacoes.length >= 2 &&
          transacoes[0].descricao === 'devolve' &&
          transacoes[0].tipo === 'd' &&
          transacoes[1].descricao === 'toma' &&
          transacoes[1].tipo === 'c';
      },
    });

    // Testing invalid requests with various incorrect inputs.
    const invalidRequests = [
      { valor: 1.2, tipo: 'd', descricao: 'devolve', expectedStatus: 422 },
      { valor: 1, tipo: 'x', descricao: 'devolve', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: '123456789 e mais', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: '', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: null, expectedStatus: 422 },
    ];

    invalidRequests.forEach((req) => {
      url = `${baseUrl}/clientes/${cliente.id}/transacoes`;
      res = http.post(url, JSON.stringify(req), {
        headers: { 'Content-Type': 'application/json' },
        tags: { endpoint: '/clientes/:id/transacoes' },
      });
      validacoesTrend.add(res.timings.duration);
      check(res, {
        [`status ${req.expectedStatus}`]: (r) =>
          r.status === req.expectedStatus || r.status === 400,
      });
    });
  });
}

// -----------------------------------------------------------------------------
// Test for client not found scenario.
// It confirms that querying for a non-existent client (id 6) returns a 404.
// -----------------------------------------------------------------------------
export function cliente_nao_encontrado() {
  const url = `${baseUrl}/clientes/6/extrato`;
  const res = http.get(url, { tags: { endpoint: '/clientes/:id/extrato' } });
  clienteNaoEncontradoTrend.add(res.timings.duration);
  check(res, {
    'status 404': (r) => r.status === 404,
  });
}
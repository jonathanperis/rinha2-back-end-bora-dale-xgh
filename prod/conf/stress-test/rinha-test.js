import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';

const baseUrl = __ENV.BASE_URL || "http://localhost:9999";

// Helper functions
const randomClienteId = () => Math.floor(Math.random() * 5) + 1;
const randomValorTransacao = () => Math.floor(Math.random() * 10000) + 1;
const randomDescricao = () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  return Array.from({ length: 10 }, () => chars[Math.floor(Math.random() * chars.length)]).join('');
};

// Validation function: checks that saldo is not below (limite * -1)
const validateSaldoLimite = (saldo, limite) => {
  return saldo >= limite * -1;
};

// Shared initial client data (same as used in the validacoes folder)
const saldosIniciaisClientes = new SharedArray('clientes', () => [
  { id: 1, limite: 1000 * 100 },
  { id: 2, limite: 800 * 100 },
  { id: 3, limite: 10000 * 100 },
  { id: 4, limite: 100000 * 100 },
  { id: 5, limite: 5000 * 100 },
]);

export const options = {
  scenarios: {
    // Validation scenarios start at 0s to use fresh state 
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
    // Load scenarios for debitos and creditos start after 10s
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
    // The extratos scenario is now executed only once with exactly 10 VUs
    extratos: {
      executor: 'per-vu-iterations',
      vus: 10,
      iterations: 1,
      startTime: '10s',
      exec: 'extratos',
    },
  },
};

export function debitos() {
  const payload = JSON.stringify({
    valor: randomValorTransacao(),
    tipo: 'd',
    descricao: randomDescricao(),
  });
  
  const res = http.post(
    `${baseUrl}/clientes/${randomClienteId()}/transacoes`,
    payload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(res, {
    'status 200 or 422': (r) => [200, 422].includes(r.status),
  });

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

export function creditos() {
  const payload = JSON.stringify({
    valor: randomValorTransacao(),
    tipo: 'c',
    descricao: randomDescricao(),
  });

  const res = http.post(
    `${baseUrl}/clientes/${randomClienteId()}/transacoes`,
    payload,
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(res, {
    'status 200': (r) => r.status === 200,
  });

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

export function extratos() {
  const res = http.get(`${baseUrl}/clientes/${randomClienteId()}/extrato`);
  
  check(res, {
    'status 200': (r) => r.status === 200,
  });

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

export function validacoes() {
  // Each VU uses a client from the shared array based on __VU index.
  const index = (__VU - 1) % saldosIniciaisClientes.length;
  const cliente = saldosIniciaisClientes[index];
  
  group('Validações cliente', () => {
    // Confirm initial client state (saldo.total should be 0)
    let res = http.get(`${baseUrl}/clientes/${cliente.id}/extrato`);
    check(res, {
      'status 200': (r) => r.status === 200,
      'limite correto': (r) => r.json('saldo.limite') === cliente.limite,
      'saldo inicial 0': (r) => r.json('saldo.total') === 0,
    });

    // Execute 2 transactions in sequence: a credit ("toma") then a debit ("devolve")
    res = http.post(
      `${baseUrl}/clientes/${cliente.id}/transacoes`,
      JSON.stringify({ valor: 1, tipo: 'c', descricao: 'toma' }),
      { headers: { 'Content-Type': 'application/json' } }
    );
    check(res, {
      'status 200': (r) => r.status === 200,
      'Consistência saldo/limite': (r) => validateSaldoLimite(r.json('saldo'), r.json('limite')),
    });

    res = http.post(
      `${baseUrl}/clientes/${cliente.id}/transacoes`,
      JSON.stringify({ valor: 1, tipo: 'd', descricao: 'devolve' }),
      { headers: { 'Content-Type': 'application/json' } }
    );
    check(res, {
      'status 200': (r) => r.status === 200,
      'Consistência saldo/limite': (r) => validateSaldoLimite(r.json('saldo'), r.json('limite')),
    });

    // Allow a pause for transactions to be properly processed
    sleep(1);

    // Validate that the latest extrato shows the two transactions in the expected order.
    res = http.get(`${baseUrl}/clientes/${cliente.id}/extrato`);
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

    // Execute invalid requests; allow either 422 or 400 as valid responses
    const invalidRequests = [
      { valor: 1.2, tipo: 'd', descricao: 'devolve', expectedStatus: 422 },
      { valor: 1, tipo: 'x', descricao: 'devolve', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: '123456789 e mais', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: '', expectedStatus: 422 },
      { valor: 1, tipo: 'c', descricao: null, expectedStatus: 422 },
    ];

    invalidRequests.forEach((req) => {
      res = http.post(
        `${baseUrl}/clientes/${cliente.id}/transacoes`,
        JSON.stringify(req),
        { headers: { 'Content-Type': 'application/json' } }
      );
      check(res, {
        [`status ${req.expectedStatus}`]: (r) =>
          r.status === req.expectedStatus || r.status === 400,
      });
    });
  });
}

export function cliente_nao_encontrado() {
  const res = http.get(`${baseUrl}/clientes/6/extrato`);
  check(res, {
    'status 404': (r) => r.status === 404,
  });
}
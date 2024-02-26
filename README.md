# Rinha de Backend - Segunda Edição

Versão C# da [rinha de backend 2ª edição - 2024/Q1](https://github.com/zanfranceschi/rinha-de-backend-2024-q1)

## Stack

- aspnet 8.0
- nginx
- postgresql

## Otimizações

- [Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options?pivots=dotnet-8-0#trimming-framework-library-features)

## Rodando o projeto

```bash
docker-compose up -d nginx
```

## Resultados

![Resultados do gatling. Todas requisições abaixo de 800ms.](docs/gatling.png)

#### ** Baseado no trabalho do colega da rinha @rafaelpadovezi (https://github.com/rafaelpadovezi/rinha-2) **
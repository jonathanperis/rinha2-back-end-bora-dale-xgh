# Rinha de Backend - Segunda Edição

Versão C# da [rinha de backend 2ª edição - 2024/Q1](https://github.com/zanfranceschi/rinha-de-backend-2024-q1). 

Esta versão é uma versão derivada da minha [versão API com cara de API](https://github.com/jonathanperis/rinha2-back-end-bora-dale), agora no modo XGH! :D

## Stack

- aspnet 9.0
- nginx
- postgresql

## Otimizações

- [AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot)
- [Trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options?pivots=dotnet-8-0#trimming-framework-library-features)

## Rodando o projeto

```bash
docker-compose up -d nginx
```

## Resultados

### Resultado do Gatling em produção (sim, é só dotnet. Mas eu gosto! XD)

![Gatling-Prod](docs/gatling-prod.png)

### Resultado do Gatling local

Todas requisições abaixo de 800ms. (Estes testes utilizaram um máximo de 250MB RAM distribuidos entre os recursos. 60% menos recurso de memória RAM do que o permitido pela rinha!

![Gatling](docs/gatling.png)

## Métricas dos testes

Métricas colhidas no Docker Desktop após a execução do teste. O teste foi executado em um Mac Mini M1 16GB RAM/512GB SSD.

- Banco de dados (Postgresql)

![Banco de dados](docs/metrica-banco-de-dados.jpeg)

- Endpoints (.NET)

![Endpoint 1 da API](docs/metrica-api-endpoint-1.jpeg)

![Endpoint 1 da API](docs/metrica-api-endpoint-2.jpeg)

- Proxy reverso (Nginx)

![Proxy reverso](docs/metrica-proxy-reverso.jpeg)

Este repositorio foi desenvolvido utilizando de minha experiencia profissional e inspirado nos seguintes colegas da rinha:

- [rafaelpadovezi/rinha-2](https://github.com/rafaelpadovezi/rinha-2)
- [giggio/rinhaback2401-01](https://github.com/giggio/rinhaback2401-01)
- [zanfranceschi/rinha-de-backend-2024-q1-zan-dotnet](https://github.com/zanfranceschi/rinha-de-backend-2024-q1-zan-dotnet)
- [offpepe/rinha-2024-q1](https://github.com/offpepe/rinha-2024-q1)
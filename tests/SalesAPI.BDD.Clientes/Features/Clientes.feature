#language: pt-br
Funcionalidade: Cadastro de Cliente
  Como administrador da loja
  Quero cadastrar clientes
  Para que eles possam realizar compras

Cenário: Cadastrar um cliente válido
  Quando eu cadastro o cliente "Maria Silva" com email "maria@email.com" e cpf "11122233344"
  Então a resposta deve ser status 201
  E o cliente cadastrado deve ter nome "Maria Silva"

Cenário: Listar clientes cadastrados
  Dado que existe o cliente "Pedro" com email "pedro@email.com" e cpf "22233344455"
  E que existe o cliente "Ana" com email "ana@email.com" e cpf "33344455566"
  Quando eu listo os clientes
  Então a resposta deve ser status 200
  E a lista deve conter 2 clientes

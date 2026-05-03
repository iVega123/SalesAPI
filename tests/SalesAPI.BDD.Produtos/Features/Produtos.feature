#language: pt-br
Funcionalidade: Cadastro de Produto e Estoque
  Como administrador da loja
  Quero cadastrar produtos e ajustar seu estoque
  Para que estejam disponíveis para venda

Cenário: Cadastrar um produto novo começa com estoque zero
  Quando eu cadastro o produto "Caderno" com preço 15.90
  Então a resposta deve ser status 201
  E o produto deve ter estoque 0

Cenário: Ajustar estoque de um produto
  Dado que existe o produto "Lápis" com preço 2.50
  Quando eu ajusto o estoque do produto "Lápis" para 100 unidades
  Então a resposta deve ser status 200
  E o produto "Lápis" deve ter estoque 100

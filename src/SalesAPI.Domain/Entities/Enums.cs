namespace SalesAPI.Domain.Entities;

public enum TipoPessoa { Fisica = 1, Juridica = 2 }

public enum StatusCliente { Ativo = 1, Inativo = 2, Bloqueado = 3 }

public enum TipoEndereco { Residencial = 1, Comercial = 2, Entrega = 3 }

public enum StatusProduto { Ativo = 1, Inativo = 2 }

public enum AcaoPermissao { Visualizar = 1, Criar = 2, Editar = 3, Excluir = 4 }

public enum StatusFornecedor { Ativo = 1, Inativo = 2 }

public enum TipoMovimentacao { Entrada = 1, Saida = 2, Ajuste = 3, Transferencia = 4, Perda = 5 }

public enum StatusCompra { Rascunho = 1, Confirmada = 2, Recebida = 3, Cancelada = 4 }

public enum StatusVenda { Orcamento = 1, Confirmada = 2, Cancelada = 3 }

public enum StatusParcela { Pendente = 1, Paga = 2, Vencida = 3, Cancelada = 4 }

public enum StatusContaPagar { Pendente = 1, Paga = 2, Vencida = 3, Cancelada = 4 }

public enum StatusCaixa { Aberto = 1, Fechado = 2 }
public enum TipoMovimentoCaixa { Abertura = 1, Sangria = 2, Suprimento = 3, Pagamento = 4, Fechamento = 5 }
public enum FormaPagamento { Dinheiro = 1, CartaoCredito = 2, CartaoDebito = 3, Pix = 4, Cheque = 5, Crediario = 6 }
public enum StatusVendaPdv { Finalizada = 1, Cancelada = 2 }

public enum StatusContaReceber { Pendente = 1, Paga = 2, Vencida = 3, Cancelada = 4 }
public enum TipoLancamento { Receita = 1, Despesa = 2 }
public enum TipoConta { Corrente = 1, Poupanca = 2, CartaoCredito = 3 }

public enum TipoNotaFiscal { NFe = 1, NFCe = 2 }
public enum StatusNotaFiscal { Rascunho = 1, Emitida = 2, Cancelada = 3, Rejeitada = 4 }

public enum ClassificacaoAbc { A = 1, B = 2, C = 3 }

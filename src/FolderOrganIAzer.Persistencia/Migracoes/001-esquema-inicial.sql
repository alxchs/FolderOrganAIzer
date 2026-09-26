CREATE TABLE raiz (
  id               INTEGER PRIMARY KEY,
  caminho          TEXT    NOT NULL,
  volume_serial    INTEGER NOT NULL,
  file_id          BLOB    NOT NULL,
  sistema_arquivos TEXT    NOT NULL CHECK (sistema_arquivos IN ('NTFS','ReFS')),
  criada_em        TEXT    NOT NULL,
  UNIQUE (volume_serial, file_id)
);

CREATE TABLE execucao (
  id                    TEXT    PRIMARY KEY,
  raiz_id               INTEGER NOT NULL REFERENCES raiz(id),
  estado                TEXT    NOT NULL CHECK (estado IN (
                          'CRIADA','VALIDANDO','AGUARDANDO_CONFIRMACAO','INVENTARIANDO',
                          'EXTRAINDO','AGUARDANDO_CUSTO','CLASSIFICANDO','REFINANDO','AGUARDANDO_REVISAO',
                          'PLANEJANDO','CRIANDO_PASTAS','MOVENDO','FINALIZANDO',
                          'CONCLUIDA','PAUSADA','CANCELADA','FALHOU')),
  estado_antes_pausa    TEXT,
  confirmada_pelo_usuario_em TEXT,
  config_json           TEXT    NOT NULL,
  versao_app            TEXT    NOT NULL,
  versao_taxonomia      INTEGER NOT NULL,
  versao_prompt         INTEGER NOT NULL,
  dono_pid              INTEGER,
  dono_inicio_processo  TEXT,
  heartbeat_em          TEXT,
  portao_expira_em      TEXT,
  criada_em             TEXT    NOT NULL,
  atualizada_em         TEXT    NOT NULL,
  encerrada_em          TEXT,
  motivo_falha          TEXT
);

CREATE TABLE arquivo (
  id              INTEGER PRIMARY KEY,
  execucao_id     TEXT    NOT NULL REFERENCES execucao(id),
  nome            TEXT    NOT NULL,
  tamanho         INTEGER NOT NULL,
  criado_fs       TEXT    NOT NULL,
  modificado_fs   TEXT    NOT NULL,
  atributos       INTEGER NOT NULL,
  volume_serial   INTEGER NOT NULL,
  file_id         BLOB    NOT NULL,
  sha256          BLOB,
  extensao        TEXT    NOT NULL,
  formato_real    TEXT,
  somente_nuvem   INTEGER NOT NULL DEFAULT 0,
  estado          TEXT    NOT NULL CHECK (estado IN (
                    'INVENTARIADO','EXCLUIDO','EXTRAIDO','FALHA_EXTRACAO',
                    'CLASSIFICADO','NAO_CLASSIFICADO','MOVIDO','MANTIDO_NA_RAIZ',
                    'ALTERADO','DESAPARECIDO')),
  motivo          TEXT,
  UNIQUE (execucao_id, nome),
  UNIQUE (execucao_id, volume_serial, file_id)
);

CREATE TABLE extracao (
  arquivo_id      INTEGER PRIMARY KEY REFERENCES arquivo(id),
  metodo          TEXT    NOT NULL,
  texto           TEXT,
  paginas_total   INTEGER,
  paginas_lidas   INTEGER,
  idioma          TEXT,
  metadados_json  TEXT    NOT NULL,
  sinais_json     TEXT    NOT NULL,
  usa_imagem      INTEGER NOT NULL DEFAULT 0,
  duracao_ms      INTEGER NOT NULL,
  erro            TEXT
);

CREATE TABLE classificacao (
  id                   INTEGER PRIMARY KEY,
  arquivo_id           INTEGER NOT NULL REFERENCES arquivo(id),
  tentativa            INTEGER NOT NULL CHECK (tentativa BETWEEN 1 AND 3),
  fonte                TEXT    NOT NULL CHECK (fonte IN ('REGRA','IA','CACHE','USUARIO')),
  categoria            TEXT,
  subcategoria         TEXT,
  atributos_json       TEXT    NOT NULL,
  confianca            REAL    NOT NULL CHECK (confianca BETWEEN 0 AND 1),
  justificativa        TEXT,
  provedor             TEXT,
  modelo               TEXT,
  tokens_entrada       INTEGER,
  tokens_saida         INTEGER,
  tokens_cache_leitura INTEGER,
  custo_estimado_usd   REAL,
  id_requisicao        TEXT,
  vigente              INTEGER NOT NULL DEFAULT 1,
  criada_em            TEXT    NOT NULL
);
CREATE UNIQUE INDEX ux_classificacao_vigente ON classificacao(arquivo_id) WHERE vigente = 1;

CREATE TABLE cache_classificacao (
  sha256            BLOB    NOT NULL,
  versao_taxonomia  INTEGER NOT NULL,
  versao_prompt     INTEGER NOT NULL,
  modelo            TEXT    NOT NULL,
  resultado_json    TEXT    NOT NULL,
  provedor          TEXT    NOT NULL,
  criada_em         TEXT    NOT NULL,
  PRIMARY KEY (sha256, versao_taxonomia, versao_prompt, provedor, modelo)
);

CREATE TABLE pasta_gerada (
  id                  INTEGER PRIMARY KEY,
  raiz_id             INTEGER NOT NULL REFERENCES raiz(id),
  marcador_guid       TEXT    NOT NULL UNIQUE,
  nivel               INTEGER NOT NULL CHECK (nivel IN (1,2)),
  pai_id              INTEGER REFERENCES pasta_gerada(id),
  nome                TEXT    NOT NULL,
  caminho_relativo    TEXT    NOT NULL,
  volume_serial       INTEGER NOT NULL,
  file_id             BLOB    NOT NULL,
  criada_por_execucao TEXT    NOT NULL REFERENCES execucao(id),
  criada_em           TEXT    NOT NULL,
  removida_em         TEXT,
  CHECK ((nivel = 1 AND pai_id IS NULL) OR (nivel = 2 AND pai_id IS NOT NULL))
);

CREATE TABLE pasta_planejada (
  id                 INTEGER PRIMARY KEY,
  execucao_id        TEXT    NOT NULL REFERENCES execucao(id),
  nivel              INTEGER NOT NULL CHECK (nivel IN (1,2)),
  pai_planejada_id   INTEGER REFERENCES pasta_planejada(id),
  nome               TEXT    NOT NULL,
  caminho_relativo   TEXT    NOT NULL,
  categoria          TEXT    NOT NULL,
  subcategoria       TEXT,
  estado             TEXT    NOT NULL CHECK (estado IN (
                       'PLANEJADA','CRIADA','REUTILIZADA','FALHOU','REMOVIDA_VAZIA')),
  marcador_guid      TEXT    NOT NULL UNIQUE,
  pasta_gerada_id    INTEGER REFERENCES pasta_gerada(id),
  UNIQUE (execucao_id, caminho_relativo),
  CHECK ((nivel = 1 AND pai_planejada_id IS NULL) OR (nivel = 2 AND pai_planejada_id IS NOT NULL))
);

CREATE TABLE movimento (
  id                 INTEGER PRIMARY KEY,
  execucao_id        TEXT    NOT NULL REFERENCES execucao(id),
  arquivo_id         INTEGER NOT NULL UNIQUE REFERENCES arquivo(id),
  pasta_planejada_id INTEGER NOT NULL REFERENCES pasta_planejada(id),
  ordem              INTEGER NOT NULL,
  lote               INTEGER NOT NULL,
  caminho_origem     TEXT    NOT NULL,
  nome_destino       TEXT    NOT NULL,
  caminho_destino    TEXT    NOT NULL,
  estado             TEXT    NOT NULL CHECK (estado IN (
                       'PENDENTE','INTENCAO','CONCLUIDO','PULADO','FALHOU')),
  tentativas         INTEGER NOT NULL DEFAULT 0,
  erro               TEXT,
  atualizado_em      TEXT    NOT NULL,
  UNIQUE (execucao_id, ordem)
);

CREATE TABLE lote_ia (
  id            INTEGER PRIMARY KEY,
  id_lote       TEXT    UNIQUE,
  execucao_id   TEXT    NOT NULL REFERENCES execucao(id),
  tentativa     INTEGER NOT NULL CHECK (tentativa IN (1,2)),
  estado        TEXT    NOT NULL CHECK (estado IN (
                  'PREPARADO','ENVIADO','ENCERRADO','RESULTADOS_GRAVADOS','EXPIRADO','CANCELADO')),
  arquivos_json TEXT    NOT NULL,
  preparado_em  TEXT    NOT NULL,
  enviado_em    TEXT,
  encerrado_em  TEXT
);

CREATE TABLE historico (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  execucao_id  TEXT    NOT NULL REFERENCES execucao(id),
  ocorrido_em  TEXT    NOT NULL,
  tipo         TEXT    NOT NULL,
  arquivo_id   INTEGER REFERENCES arquivo(id),
  origem       TEXT,
  destino      TEXT,
  sha256       BLOB,
  detalhes_json TEXT   NOT NULL,
  hash_encadeado BLOB  NOT NULL
);
CREATE TRIGGER historico_sem_update BEFORE UPDATE ON historico
  BEGIN SELECT RAISE(ABORT, 'historico e somente insercao'); END;
CREATE TRIGGER historico_sem_delete BEFORE DELETE ON historico
  BEGIN SELECT RAISE(ABORT, 'historico e somente insercao'); END;

CREATE TABLE evento (
  seq          INTEGER PRIMARY KEY AUTOINCREMENT,
  execucao_id  TEXT    NOT NULL REFERENCES execucao(id),
  tipo         TEXT    NOT NULL,
  payload_json TEXT    NOT NULL,
  criado_em    TEXT    NOT NULL
);

CREATE INDEX ix_arquivo_estado     ON arquivo(execucao_id, estado);
CREATE INDEX ix_movimento_fila     ON movimento(execucao_id, estado, ordem);
CREATE INDEX ix_historico_execucao ON historico(execucao_id, id);
CREATE INDEX ix_pasta_gerada_id    ON pasta_gerada(volume_serial, file_id);
CREATE INDEX ix_pasta_gerada_rel   ON pasta_gerada(raiz_id, caminho_relativo);
CREATE INDEX ix_evento_execucao    ON evento(execucao_id, seq);

DROP TABLE IF EXISTS order_items CASCADE;
DROP TABLE IF EXISTS orders      CASCADE;
DROP TABLE IF EXISTS cart_items  CASCADE;
DROP TABLE IF EXISTS carts       CASCADE;
DROP TABLE IF EXISTS products    CASCADE;
DROP TABLE IF EXISTS categories  CASCADE;
DROP TABLE IF EXISTS users       CASCADE;

CREATE TABLE categories (
    id   SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO categories (name, slug) VALUES
('Корма',       'food'),
('Игрушки',     'toys'),
('Аксессуары',  'accessories'),
('Лекарства',   'medicine'),
('Гигиена',     'hygiene');

CREATE TABLE products (
    id          SERIAL PRIMARY KEY,
    category_id INTEGER NOT NULL REFERENCES categories(id),
    name        VARCHAR(200) NOT NULL,
    description TEXT,
    price       DECIMAL(10,2) NOT NULL CHECK (price >= 0),
    stock       INTEGER NOT NULL DEFAULT 0,
    animal_type VARCHAR(20) NOT NULL,
    image_url   VARCHAR(300),
    created_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO products (category_id, name, description, price, stock, animal_type, image_url) VALUES
-- Корма
(1, 'Royal Canin Adult для кошек 2 кг',  'Сбалансированный сухой корм для взрослых кошек',           1450.00, 30, 'cat',    '/img/p1.jpg'),
(1, 'Pro Plan для собак 3 кг',           'Сухой корм для взрослых собак средних пород',              1890.00, 25, 'dog',    '/img/p2.jpg'),
(1, 'Whiskas пауч с курицей 85 г',       'Влажный корм для кошек, упаковка 24 шт',                    890.00, 60, 'cat',    '/img/p3.jpg'),
(1, 'Корм для попугаев Padovan 1 кг',    'Зерновая смесь для волнистых попугаев',                     420.00, 40, 'bird',   '/img/p4.jpg'),
(1, 'Tetra корм для рыбок 100 мл',       'Универсальный корм-хлопья для аквариумных рыб',             350.00, 50, 'fish',   '/img/p5.jpg'),

-- Игрушки
(2, 'Мячик для собак резиновый',          'Прыгучий мяч из плотной резины, 7 см',                      250.00, 100, 'dog',    '/img/p6.jpg'),
(2, 'Удочка-дразнилка для кошек',         'С перьями и колокольчиком',                                 320.00, 80,  'cat',    '/img/p7.jpg'),
(2, 'Лабиринт для хомяка',                'Пластиковый туннель-лабиринт',                              780.00, 15,  'rodent', '/img/p8.jpg'),
(2, 'Когтеточка-столбик 50 см',           'Сизалевая когтеточка с игрушкой',                          1290.00, 20,  'cat',    '/img/p9.jpg'),

-- Аксессуары
(3, 'Ошейник кожаный M',                  'Регулируемый, для собак среднего размера',                  890.00, 35,  'dog',    '/img/p10.jpg'),
(3, 'Поводок-рулетка 5 м',                'Для собак до 25 кг',                                       1450.00, 18,  'dog',    '/img/p11.jpg'),
(3, 'Переноска для кошки',                'Пластиковая, до 8 кг',                                     2390.00, 10,  'cat',    '/img/p12.jpg'),
(3, 'Аквариум 30 л с фильтром',           'Стеклянный, со встроенной подсветкой',                     4500.00, 5,   'fish',   '/img/p13.jpg'),

-- Лекарства
(4, 'Капли от блох Bayer',                'Противопаразитарные капли на холку',                        680.00, 45,  'universal', '/img/p14.jpg'),
(4, 'Витамины Beaphar для кошек',         'Мультивитаминный комплекс, 180 таблеток',                   590.00, 30,  'cat',       '/img/p15.jpg'),
(4, 'Средство от глистов для собак',      'Таблетки, 6 шт в упаковке',                                 420.00, 25,  'dog',       '/img/p16.jpg'),

-- Гигиена
(5, 'Шампунь для собак 250 мл',           'Гипоаллергенный, для всех типов шерсти',                    390.00, 40,  'dog',    '/img/p17.jpg'),
(5, 'Наполнитель для кошек 5 л',          'Бентонитовый комкующийся',                                  450.00, 70,  'cat',    '/img/p18.jpg'),
(5, 'Пелёнки впитывающие 60x60, 30 шт',   'Одноразовые пелёнки для щенков и животных',                 690.00, 35,  'universal', '/img/p19.jpg'),
(5, 'Щётка-фурминатор для кошек',         'Для удаления подшёрстка',                                  1890.00, 12,  'cat',    '/img/p20.jpg');

CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    email         VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(200) NOT NULL,
    name          VARCHAR(100) NOT NULL,
    phone         VARCHAR(20),
    created_at    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO users (email, password_hash, name, phone) VALUES
('aigul@example.com',  'demo_hash_1', 'Айгуль Байдавлетова', '+7-900-000-00-01'),
('ivan@example.com',   'demo_hash_2', 'Иван Иванов',         '+7-900-000-00-02'),
('maria@example.com',  'demo_hash_3', 'Мария Сидорова',      '+7-900-000-00-03');

CREATE TABLE carts (
    id      SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE
);

CREATE TABLE cart_items (
    id         SERIAL PRIMARY KEY,
    cart_id    INTEGER NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    product_id INTEGER NOT NULL REFERENCES products(id),
    quantity   INTEGER NOT NULL DEFAULT 1 CHECK (quantity > 0),
    UNIQUE (cart_id, product_id)
);

INSERT INTO carts (user_id) VALUES (1), (2);
INSERT INTO cart_items (cart_id, product_id, quantity) VALUES
(1, 1, 2),
(1, 7, 1);

CREATE TABLE orders (
    id         SERIAL PRIMARY KEY,
    user_id    INTEGER NOT NULL REFERENCES users(id),
    total      DECIMAL(10,2) NOT NULL,
    status     VARCHAR(20) NOT NULL DEFAULT 'new',
    address    VARCHAR(300) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_items (
    id         SERIAL PRIMARY KEY,
    order_id   INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id INTEGER NOT NULL REFERENCES products(id),
    quantity   INTEGER NOT NULL CHECK (quantity > 0),
    price      DECIMAL(10,2) NOT NULL
);

INSERT INTO orders (user_id, total, status, address) VALUES
(1, 3340.00, 'delivered', 'Москва, ул. Тверская, 1'),
(2, 1890.00, 'paid',      'Москва, ул. Арбат, 10'),
(1, 1140.00, 'new',       'Москва, ул. Тверская, 1');

INSERT INTO order_items (order_id, product_id, quantity, price) VALUES
(1, 1, 2, 1450.00),
(1, 9, 1, 1290.00),
(1, 14, 1, 680.00),
(2, 2, 1, 1890.00),
(3, 18, 2, 450.00),
(3, 17, 1, 390.00);

CREATE INDEX idx_products_category ON products(category_id);
CREATE INDEX idx_products_animal   ON products(animal_type);
CREATE INDEX idx_orders_user       ON orders(user_id);
CREATE INDEX idx_orders_status     ON orders(status);

SELECT 'Готово' AS status;
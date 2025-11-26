import { Routes } from '@angular/router';
import { Register } from './components/authentification/register/register';
import { Login } from './components/authentification/login/login';
import { ProductList } from './components/shop/product-list/product-list';
import { Cart } from './components/shop/cart/cart';
import { Orders } from './components/shop/orders/orders';
import { Orders as AdminOrders } from './components/admin/orders/orders';
import { Products as AdminProducts } from './components/admin/products/products';

export const routes: Routes = [
	{ path: '', component: ProductList },
	{ path: 'cart', component: Cart },
	{ path: 'orders', component: Orders },
	{ path: 'admin/orders', component: AdminOrders },
	{ path: 'admin/products', component: AdminProducts },
	{ path: 'register', component: Register },
	{ path: 'login', component: Login },
];

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CustomerOrdersPage } from './customer-orders.page';
import { RoleService } from '../../core/services/role.service';

describe('CustomerOrdersPage', () => {
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    localStorage.removeItem('wms.selected-role');

    await TestBed.configureTestingModule({
      imports: [CustomerOrdersPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.removeItem('wms.selected-role');
  });

  it('should submit a customer order for warehouse staff', () => {
    const roleService = TestBed.inject(RoleService);
    roleService.setSelectedRole('WarehouseStaff');

    const fixture = TestBed.createComponent(CustomerOrdersPage);
    fixture.detectChanges();

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/products/stock')
      .flush([
        {
          productId: '11111111-1111-1111-1111-111111111111',
          sku: 'SKU-001',
          name: 'Warehouse Widget',
          quantityOnHand: 8,
        },
      ]);

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/customer-orders/open')
      .flush([]);

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const customerNameInput = compiled.querySelector(
      'input[formcontrolname="customerName"]',
    ) as HTMLInputElement;
    const productSelect = compiled.querySelector(
      'select[formcontrolname="productId"]',
    ) as HTMLSelectElement;
    const unitPriceInput = compiled.querySelector(
      'input[formcontrolname="unitPriceAmount"]',
    ) as HTMLInputElement;
    const createForm = compiled.querySelector('form') as HTMLFormElement;

    customerNameInput.value = 'Ainsley Tools';
    customerNameInput.dispatchEvent(new Event('input'));

    productSelect.value = '11111111-1111-1111-1111-111111111111';
    productSelect.dispatchEvent(new Event('change'));

    unitPriceInput.value = '12.50';
    unitPriceInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    createForm.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const createRequest = httpTestingController.expectOne(
      (request) => request.method === 'POST' && request.url === '/api/customer-orders',
    );

    expect(createRequest.request.body).toEqual({
      customer: {
        name: 'Ainsley Tools',
        email: null,
        phone: null,
      },
      lines: [
        {
          productId: '11111111-1111-1111-1111-111111111111',
          quantity: 1,
          unitPrice: {
            amount: 12.5,
            currency: 'GBP',
          },
        },
      ],
    });

    createRequest.flush({
      customerOrderId: '33333333-3333-3333-3333-333333333333',
      status: 'Confirmed',
      createdAt: '2026-04-06T09:00:00Z',
      lines: [
        {
          productId: '11111111-1111-1111-1111-111111111111',
          quantity: 1,
          unitPrice: {
            amount: 12.5,
            currency: 'GBP',
          },
        },
      ],
      totalAmount: {
        amount: 12.5,
        currency: 'GBP',
      },
    });

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/products/stock')
      .flush([
        {
          productId: '11111111-1111-1111-1111-111111111111',
          sku: 'SKU-001',
          name: 'Warehouse Widget',
          quantityOnHand: 7,
        },
      ]);

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/customer-orders/open')
      .flush([
        {
          customerOrderId: '33333333-3333-3333-3333-333333333333',
          status: 'Confirmed',
          createdAt: '2026-04-06T09:00:00Z',
          lines: [
            {
              productId: '11111111-1111-1111-1111-111111111111',
              quantity: 1,
              unitPrice: {
                amount: 12.5,
                currency: 'GBP',
              },
            },
          ],
          totalAmount: {
            amount: 12.5,
            currency: 'GBP',
          },
        },
      ]);

    fixture.detectChanges();

    expect(compiled.textContent).toContain('Latest customer order ID');
    expect(compiled.textContent).toContain('33333333-3333-3333-3333-333333333333');
  });
});

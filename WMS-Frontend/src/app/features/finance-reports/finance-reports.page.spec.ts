import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FinanceReportsPage } from './finance-reports.page';
import { RoleService } from '../../core/services/role.service';

describe('FinanceReportsPage', () => {
  let httpTestingController: HttpTestingController;

  beforeEach(async () => {
    localStorage.removeItem('wms.selected-role');

    await TestBed.configureTestingModule({
      imports: [FinanceReportsPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.removeItem('wms.selected-role');
  });

  it('should show the administrator guard for non-admin roles', () => {
    const fixture = TestBed.createComponent(FinanceReportsPage);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Administrator role required');
  });

  it('should load finance data for administrators', () => {
    const roleService = TestBed.inject(RoleService);
    roleService.setSelectedRole('Administrator');

    const fixture = TestBed.createComponent(FinanceReportsPage);
    fixture.detectChanges();

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/transactions')
      .flush([
        {
          transactionId: '11111111-1111-1111-1111-111111111111',
          type: 'Sale',
          status: 'Posted',
          amount: { amount: 120, currency: 'GBP' },
          occurredAt: '2026-04-01T08:30:00Z',
          referenceType: 'CustomerOrder',
          referenceId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          reversalOfTransactionId: null,
        },
      ]);

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/reports/financial')
      .flush({
        from: '2026-03-01',
        to: '2026-03-31',
        totalSales: { amount: 500, currency: 'GBP' },
        totalExpenses: { amount: 200, currency: 'GBP' },
      });

    httpTestingController
      .expectOne((request) => request.method === 'GET' && request.url === '/api/reports/exports')
      .flush([
        {
          exportId: '22222222-2222-2222-2222-222222222222',
          reportType: 'FinancialSummary',
          format: 'TXT',
          generatedAt: '2026-04-01T09:15:00Z',
          filePath: '/tmp/reports/financial-summary.txt',
        },
      ]);

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Financial summary');
    expect(compiled.textContent).toContain('Posted');
    expect(compiled.textContent).toContain('/tmp/reports/financial-summary.txt');
  });
});

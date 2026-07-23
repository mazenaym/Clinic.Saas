import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MediaService } from './media.service';

describe('MediaService', () => {
  let service: MediaService;
  let http: HttpTestingController;
  let counter = 0;

  beforeEach(() => {
    TestBed.resetTestingModule(); counter = 0;
    vi.spyOn(URL, 'createObjectURL').mockImplementation(() => `blob:test-${++counter}`);
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MediaService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { http.verify(); vi.restoreAllMocks(); TestBed.resetTestingModule(); });

  it('loads protected avatar and tenant logo as blobs once per identity', async () => {
    service.onSessionChanged('user-1', 'tenant-1');
    http.expectOne('/api/media/me/avatar').flush(new Blob(['avatar'], { type: 'image/webp' }));
    http.expectOne('/api/media/tenant/logo').flush(new Blob(['logo'], { type: 'image/webp' }));
    await Promise.resolve();
    expect(service.currentAvatarUrl()).toBe('blob:test-1');
    expect(service.currentTenantLogoUrl()).toBe('blob:test-2');
    service.onSessionChanged('user-1', 'tenant-1');
    http.expectNone('/api/media/me/avatar');
  });

  it('clears and revokes previous tenant media on identity change', async () => {
    service.onSessionChanged('user-1', 'tenant-1');
    http.expectOne('/api/media/me/avatar').flush(new Blob(['a']));
    http.expectOne('/api/media/tenant/logo').flush(new Blob(['l']));
    await Promise.resolve();
    service.onSessionChanged('user-2', 'tenant-2');
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:test-1');
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:test-2');
    http.expectOne('/api/media/me/avatar').flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne('/api/media/tenant/logo').flush(null, { status: 404, statusText: 'Not Found' });
    await Promise.resolve();
    expect(service.currentAvatarUrl()).toBeNull();
    expect(service.currentTenantLogoUrl()).toBeNull();
  });

  it('uploads avatar then replaces the cached blob immediately', async () => {
    service.onSessionChanged('user-1', 'tenant-1');
    http.expectOne('/api/media/me/avatar').flush(null, { status: 404, statusText: 'Not Found' });
    http.expectOne('/api/media/tenant/logo').flush(null, { status: 404, statusText: 'Not Found' });
    await Promise.resolve();
    const promise = service.uploadCurrentAvatar(new File(['image'], 'صورة.webp', { type: 'image/webp' }));
    http.expectOne('/api/media/me/avatar').flush({ success: true, data: {}, message: 'OK', statusCode: 200 });
    await Promise.resolve();
    http.expectOne('/api/media/me/avatar').flush(new Blob(['new'], { type: 'image/webp' }));
    await promise;
    expect(service.currentAvatarUrl()).toBe('blob:test-1');
  });

  it('deletes avatar and returns to fallback state', async () => {
    service.onSessionChanged('user-1', 'tenant-1');
    http.expectOne('/api/media/me/avatar').flush(new Blob(['a']));
    http.expectOne('/api/media/tenant/logo').flush(null, { status: 404, statusText: 'Not Found' });
    await Promise.resolve();
    const promise = service.deleteCurrentAvatar();
    http.expectOne('/api/media/me/avatar').flush({ success: true, data: true, message: 'OK', statusCode: 200 });
    await promise;
    expect(service.currentAvatarUrl()).toBeNull();
    expect(URL.revokeObjectURL).toHaveBeenCalled();
  });
});

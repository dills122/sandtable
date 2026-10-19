import { MapCameraState, clampCameraFocus } from './map-camera';

describe('connected map camera', () => {
  it('locates normalized coordinates at readable zoom with the existing edge clamp', () => {
    const camera = new MapCameraState();

    expect(camera.locate(16, 84)).toBe(true);
    expect(camera.zoom()).toBe(1.5);
    expect(camera.focusX()).toBe(clampCameraFocus(16, 1.5));
    expect(camera.focusY()).toBe(clampCameraFocus(84, 1.5));

    camera.zoomBy(0.5);
    expect(camera.locate(50, 50)).toBe(true);
    expect(camera.zoom()).toBe(2);
    expect(camera.focusX()).toBe(50);
    expect(camera.focusY()).toBe(50);

    camera.fitMap();
    expect([camera.zoom(), camera.focusX(), camera.focusY()]).toEqual([1, 50, 50]);
  });

  it('ignores missing or invalid audience coordinates without changing camera', () => {
    const camera = new MapCameraState();
    camera.locate(84, 16);
    const before = [camera.zoom(), camera.focusX(), camera.focusY()];

    expect(camera.locate(undefined, 50)).toBe(false);
    expect(camera.locate(50, NaN)).toBe(false);
    expect(camera.locate(Infinity, 50)).toBe(false);
    expect(camera.locate(-1, 50)).toBe(false);
    expect(camera.locate(50, 101)).toBe(false);
    expect([camera.zoom(), camera.focusX(), camera.focusY()]).toEqual(before);
  });
});

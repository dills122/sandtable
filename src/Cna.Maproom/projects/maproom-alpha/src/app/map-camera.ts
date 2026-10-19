import { computed, signal } from '@angular/core';

export const clampCameraFocus = (focus: number, zoom: number) =>
  Math.min(100 - 50 / zoom, Math.max(50 / zoom, focus));

export const cameraTransform = (zoom: number, x: number, y: number) =>
  `translate(${50 - x * zoom}%, ${50 - y * zoom}%) scale(${zoom})`;

export const nextZoom = (zoom: number, step: number) =>
  Math.min(2, Math.max(1, Math.round((zoom + step) * 4) / 4));

export const panFocus = (focus: number, pixels: number, size: number, zoom: number) =>
  clampCameraFocus(focus - ((pixels / size) * 100) / zoom, zoom);

/** Connected presentation camera; no campaign authority or persisted state. */
export class MapCameraState {
  readonly zoom = signal(1);
  readonly focusX = signal(50);
  readonly focusY = signal(50);
  readonly transform = computed(() => cameraTransform(this.zoom(), this.focusX(), this.focusY()));

  fitMap(): void {
    this.zoom.set(1);
    this.focusX.set(50);
    this.focusY.set(50);
  }

  zoomBy(step: number): void {
    const zoom = nextZoom(this.zoom(), step);
    this.focusX.set(clampCameraFocus(this.focusX(), zoom));
    this.focusY.set(clampCameraFocus(this.focusY(), zoom));
    this.zoom.set(zoom);
  }

  panByPixels(x: number, y: number, width: number, height: number): void {
    if (this.zoom() <= 1 || width <= 0 || height <= 0) return;
    this.focusX.set(panFocus(this.focusX(), x, width, this.zoom()));
    this.focusY.set(panFocus(this.focusY(), y, height, this.zoom()));
  }
}

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EntityDesigner } from './entity-designer';

describe('EntityDesigner', () => {
  let component: EntityDesigner;
  let fixture: ComponentFixture<EntityDesigner>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EntityDesigner]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EntityDesigner);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

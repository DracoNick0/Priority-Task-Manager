// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'fixed_event.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class FixedEventAdapter extends TypeAdapter<FixedEvent> {
  @override
  final typeId = 3;

  @override
  FixedEvent read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return FixedEvent(
      id: fields[0] as String,
      listId: fields[1] as String,
      title: fields[2] as String,
      startTime: fields[3] as DateTime,
      endTime: fields[4] as DateTime,
    );
  }

  @override
  void write(BinaryWriter writer, FixedEvent obj) {
    writer
      ..writeByte(5)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.listId)
      ..writeByte(2)
      ..write(obj.title)
      ..writeByte(3)
      ..write(obj.startTime)
      ..writeByte(4)
      ..write(obj.endTime);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is FixedEventAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
